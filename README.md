# MetaRea World GUIDE

VRChatワールドに置くワールド紹介ボードです。VCCから導入・更新します。

## 導入

1. [VCC配布ページ](https://metarea-system.github.io/metarea-world-guide/)からリポジトリを追加します。
2. VCCでワールドプロジェクトへ **MetaRea World GUIDE** を追加し、Unityを開きます。
3. 自動配置・コンパイル後、**Assets/MetaRea_World_GUIDE/MetaRea World GUIDE.prefab** をHierarchyへドラッグします。

[かんたんガイド](Assets/MetaRea_World_GUIDE/README.txt)に配置・設定・ワールド内操作・更新方法をまとめています。

## 主な機能

- TOPの2枠／4枠表示と、お気に入りタブの有無を選択
- おすすめ・ランダムの再抽選（待機15秒、複数ボード・参加者で共通）
- ワールドの紹介、ポータル作成、設置者が登録するお気に入り一覧
- 8種類の配色プリセットと各部の色調整
- TOP広告の最大3行表示、広告画像・QRコード・公式リンク
- 複数ボードでのデータ・画像の共有

## 0.9.0以降の配置

プレハブ、スクリプト、画像、マテリアルなどの本体はすべて **Assets/MetaRea_World_GUIDE** に置かれます。
直下には導入用プレハブとREADME、配下にはEditor、Scripts、Internal、UI、Fontsなどのフォルダがあります。
Sceneは配布しません。Packagesに残るのはVCCの配置・更新処理と圧縮された配布データだけです。

既存の導入用プレハブとGUIDは保持します。更新対象の変更前ファイルは必要に応じて
プロジェクト直下のMetaReaWorldGuideBackupsに退避します。

## 配布の作成

本体の編集元はAssets/MetaRea_World_GUIDEです。
package.jsonとMetaReaOnlineSetup.GuideVersionを同じバージョンにし、
Unityでコンパイル・プレハブの確認後に公開します。
CIはci/build_vcc_payload.pyでGUID・Scene混入・バージョンを検査し、
本体をVCCパッケージ内のPayload~へ圧縮します。配布物はVCC用ZIPのみです。
