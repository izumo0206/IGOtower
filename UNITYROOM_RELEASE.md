# IGOtower — unityroom 公開チェックリスト

## ゲーム説明

四方を囲んで敵を捕獲する、ターン制の配置タワーディフェンスです。

- パズルモード: チュートリアルと全8ステージ
- 操作: マウスでユニットを選び、盤面をクリックして配置。方向を選んで決定します。

## WebGL ビルド

1. Unity 2022.3.40f1 で WebGL に切り替える。
2. Development Build をオフにする。
3. Player Settings の Compression Format は Gzip、Decompression Fallback はオフにする。
4. `Build/WebGL` へビルドし、Chrome と Firefox で動作確認する。
5. unityroomのゲームページにビルドフォルダをアップロードし、更新時は必要に応じてサーバーキャッシュをクリアする。

## クレジット

本プロジェクトに含まれる外部アセットのライセンス・利用規約を、公開前に必ず確認してください。

- TextMesh Pro
- SPUM
- Hisa Cube Animal
- Space Exploration GUI Kit

unityroomの説明欄にも、各アセットの正式な配布元と要求される表記を掲載してください。
