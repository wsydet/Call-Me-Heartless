# 项目协作说明

## 飞书 CLI（lark-cli）

- 本机已安装 `lark-cli`。2026-09-22 已在沙箱外运行 `--version` 验证成功，版本为 `1.0.93`；后续使用时以实际版本输出为准。
- Windows 命令入口（用户 `wuyu` 的环境）：`C:\Users\wuyu\AppData\Roaming\npm\lark-cli.cmd`，同目录也有 `lark-cli.ps1`。
- 另一套已验证入口（用户 `31307` 的环境）：`C:\Users\31307\.dsh-runtime\node\lark-cli.cmd`，同目录也有 `lark-cli.ps1`。该环境没有系统级 Node，只使用 DSH 自带运行时，其 npm 全局前缀就是 `C:\Users\31307\.dsh-runtime\node`，该目录本身已在 PATH 上。2026-09-25 装的是官方 `@larksuite/cli` 1.0.96（二进制由 postinstall 自动下载到包内 `bin\lark-cli.exe`），`lark-cli --version` 与 `Get-Command lark-cli` 均已验证通过。该环境已于 2026-09-25 完成 `config init` 与 `auth login --recommend`：应用 `cli_aacd99f2eff81bd8`（brand `feishu`），登录用户 吴彧（`ou_14aaef1cedf596d1415d61852f3ed244`），`auth status` 返回 `User identity: ready`、`whoami` 以 user 身份可用、`doctor` 全部 pass。CLI 状态目录为 `C:\Users\31307\.lark-cli`（`config.json` 内含 App Secret，不得读取输出或外传）。
- 该状态目录在工作区之外：沙箱内运行 `lark-cli` 时，凡涉及写盘的动作（token 刷新、`config init`、`auth login`）都会被沙箱拒绝（已验证 `C:\Users\31307` 下创建目录被拒），表现为命令长时间无输出或轮询持续报错。此时应申请沙箱外执行，不要重装 CLI，也不要重新走一遍授权流程。
- 当前环境的沙箱可能限制访问 npm 全局安装目录，导致 `Get-Command lark-cli` 找不到命令、目录读取被拒绝，或 `npm list -g` 显示为空。这些结果不足以判断 CLI 未安装。
- 使用飞书相关技能前，先按技能要求检查命令及版本。若命令名不可用，检查上述已知入口；若遇到沙箱访问限制，申请在沙箱外执行下列版本检查，不要绕过权限，也不要直接建议重新安装：

  ```powershell
  # 用户 wuyu 的环境
  & 'C:\Users\wuyu\AppData\Roaming\npm\lark-cli.cmd' --version
  # 用户 31307 的环境
  & 'C:\Users\31307\.dsh-runtime\node\lark-cli.cmd' --version
  ```

- 此记录仅证明 CLI 曾安装且可执行，不代表当前登录、应用配置或目标文档权限已经验证。实际业务操作仍按对应 `lark-*` 技能检查身份与权限，不输出密钥或令牌。
- 不自动重装 CLI，不因命令发现失败而改用浏览器绕过技能要求。其他对话可能仍需取得各自的执行权限；本文不授予沙箱外执行权限。
