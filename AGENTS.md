# AGENTS.md

## グローバル方針
- 応答は日本語で行う。
- 変更は小さく、目的に沿って最小限にする。
- 不要な依存関係は追加しない。

### 推奨
- codex のソース読み取り時は UTF-8 で読み込むこと。
- UTF-8 指定で再読み取りを優先して確認すること。

## Unity / UI 方針
- UI の参照が取れない場合に、ランタイムで代替 UI を生成するフォールバック実装はしないこと。
- 参照未設定でエラーになってもよい。Prefab 側の参照設定ミスとして扱うこと。
- Prefab はユーザーから明示的な指示があるまで触らないこと。

# GitHub Account Rules

- GitHubは personal / work を SSH で使い分ける
- `git@github.com:...` と HTTPS は使用禁止
- 必ずホスト別名を使う
- 設定を追加・修正する場合は確認すること

personal: git@github-personal:USER/REPO.git
work:     git@github-work:ORG/REPO.git

check:
git remote -v

fix:
git remote set-url origin git@github-personal:USER/REPO.git
git remote set-url origin git@github-work:ORG/REPO.git
