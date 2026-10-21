"""Export approved navigator expressions from the original PSD. Requires Pillow.

No painting or AI generation: composite the source's normal RGBA layers, retaining
the same crop and scale for every expression. The PSD is never modified.
"""
import hashlib
import re
import struct
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/Material/navi.psd'
OUTPUT = ROOT / 'Assets/Art/TutorialNavigator'
FACES = {'normal': 4, 'speaking': 5, 'surprised': 7, 'happy': 10, 'thinking': 11}
CROP = (400, 280, 1820, 1930)


def unpack(data, width, height):
    compression = struct.unpack_from('>H', data)[0]
    if compression == 0:
        return data[2:]
    if compression != 1:
        raise ValueError('Expected raw or PackBits layer data')
    data = data[2 + 2 * height:]
    result = bytearray()
    pos = 0
    while pos < len(data):
        size = data[pos]
        pos += 1
        if size <= 127:
            result.extend(data[pos:pos + size + 1])
            pos += size + 1
        elif size >= 129:
            result.extend(data[pos:pos + 1] * (257 - size))
            pos += 1
    if len(result) != width * height:
        raise ValueError('Invalid decompressed layer size')
    return bytes(result)


def read_layers():
    data = SOURCE.read_bytes()
    signature, version, _, channels, height, width, depth, mode = struct.unpack('>4sH6sHIIHH', data[:26])
    if (signature, version, depth, mode) != (b'8BPS', 1, 8, 3):
        raise ValueError('Expected 8-bit RGB PSD version 1')
    pos = 26
    for _ in range(2):
        length = struct.unpack_from('>I', data, pos)[0]
        pos += 4 + length
    pos += 8  # Layer/mask section length, then layer information length.
    count = abs(struct.unpack_from('>h', data, pos)[0])
    pos += 2
    layers = []
    for index in range(count):
        top, left, bottom, right, count_channels = struct.unpack_from('>iiiiH', data, pos)
        pos += 18
        channel_info = []
        for _ in range(count_channels):
            channel_info.append(struct.unpack_from('>hI', data, pos))
            pos += 6
        _, blend, opacity, clipping, flags, _ = struct.unpack_from('>4s4sBBBB', data, pos)
        pos += 12
        extra = struct.unpack_from('>I', data, pos)[0]
        pos += 4
        end = pos + extra
        for _ in range(2):
            length = struct.unpack_from('>I', data, pos)[0]
            pos += 4 + length
        length = data[pos]
        pos += ((length + 4) // 4) * 4
        name = ''
        while pos + 12 <= end:
            _, key, length = struct.unpack_from('>4s4sI', data, pos)
            pos += 12
            chunk = data[pos:pos + length]
            if key == b'luni':
                chars = struct.unpack_from('>I', chunk)[0]
                name = chunk[4:4 + chars * 2].decode('utf-16be')
            pos += length + length % 2
        pos = end
        if blend != b'norm' or clipping:
            raise ValueError(f'Unsupported layer blending at layer {index}')
        layers.append(dict(name=name, box=(left, top, right, bottom),
                           visible=not flags & 2, opacity=opacity, channels=channel_info))
    if len(layers) != 24 or any(layers[i]['name'] != '顔線画' for i in range(4, 12)):
        raise ValueError('PSD layers changed; review expression mapping before export')
    for layer in layers:
        left, top, right, bottom = layer['box']
        size = (right - left, bottom - top)
        channels = {}
        for channel, length in layer['channels']:
            channels[channel] = Image.frombytes('L', size, unpack(data[pos:pos + length], *size))
            pos += length
        layer['image'] = Image.merge('RGBA', tuple(channels[c] for c in (0, 1, 2, -1)))
        layer['image'].putalpha(layer['image'].getchannel('A').point(lambda a: a * layer['opacity'] // 255))
    return layers, (width, height)


def guid(name):
    return hashlib.md5(('IGOtower/TutorialNavigator/' + name).encode()).hexdigest()


def write_meta(path, name):
    meta_path = Path(str(path) + '.meta')
    if meta_path.exists():
        return  # Preserve Unity GUIDs and any user-adjusted import settings.
    meta = Path(str(SOURCE) + '.meta').read_text(encoding='utf-8-sig')
    meta = re.sub(r'^guid: .*', 'guid: ' + guid(name), meta, flags=re.M)
    meta = re.sub(r'^(\s*spriteID:) .*', lambda m: m[1] + ' ' + guid(name + '/sprite'), meta, flags=re.M)
    for field, value in {'textureType': 8, 'spriteMode': 1, 'enableMipMap': 0,
                         'isReadable': 0, 'spriteMeshType': 0, 'maxTextureSize': 1024,
                         'alphaIsTransparency': 1, 'textureCompression': 0}.items():
        meta = re.sub(r'^(\s*' + field + r':) .*', lambda m: m[1] + ' ' + str(value), meta, flags=re.M)
    meta_path.write_text(meta, encoding='utf8')


def main():
    layers, size = read_layers()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, face in FACES.items():
        canvas = Image.new('RGBA', size)
        # This source stores layers bottom-to-top; checked against its merged preview.
        for index, layer in enumerate(layers):
            if (layer['visible'] and not 4 <= index <= 11) or index == face:
                canvas.alpha_composite(layer['image'], layer['box'][:2])
        canvas = canvas.crop(CROP)
        canvas.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
        path = OUTPUT / f'navi_{name}.png'
        canvas.save(path)
        write_meta(path, name)
    tail = Image.new('RGBA', (96, 96))
    ImageDraw.Draw(tail).polygon([(0, 0), (95, 48), (0, 96)], fill='white')
    path = OUTPUT / 'speech_tail.png'
    tail.save(path)
    write_meta(path, 'speech_tail')
    panel = Image.new('RGBA', (96, 96))
    ImageDraw.Draw(panel).rounded_rectangle((0, 0, 95, 95), radius=20, fill='white')
    path = OUTPUT / 'white_panel.png'
    panel.save(path)
    write_meta(path, 'white_panel')
    panel_meta = Path(str(path) + '.meta')
    meta = panel_meta.read_text(encoding='utf8')
    meta = re.sub(r'^(\s*spriteBorder:) .*', lambda m: m[1] + ' {x: 24, y: 24, z: 24, w: 24}', meta, flags=re.M)
    panel_meta.write_text(meta, encoding='utf8')
    for folder in [OUTPUT.parent, OUTPUT]:
        path = Path(str(folder) + '.meta')
        if not path.exists():
            path.write_text(f'fileFormatVersion: 2\nguid: {guid(folder.name)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf8')
    print(f'Exported {len(FACES)} aligned expressions, speech tail and white panel to {OUTPUT}')
    print('PSD SHA256:', hashlib.sha256(SOURCE.read_bytes()).hexdigest())


if __name__ == '__main__':
    main()
