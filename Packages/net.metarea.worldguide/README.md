# MetaRea World GUIDE

本体はPackages/net.metarea.worldguide/Core、設置用Prefab VariantとUserContentはAssets/MetaRea_World_GUIDEへ配置します。
独立した更新処理がPayload~を検証し、旧本体をバックアップしてからPackagesへ展開します。
同じGUIDの旧本体と新本体が同時にインポートされることを防ぎます。

SDKはcom.vrchat.worlds >=3.10.5。0.13.0はguide-v4配信とBOOTHリンク・プリセット配色に対応しています。
