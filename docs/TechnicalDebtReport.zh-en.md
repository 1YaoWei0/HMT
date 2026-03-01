# Technical Debt Report / 技术债务报告

> Scope: `/workspace/HMT` VSIX project (incremental quality review, no destructive rewrite).
> 范围：`/workspace/HMT` VSIX 项目（增量质量审查，不做破坏性重写）。

---

## 1) Executive Summary / 执行摘要

### EN
The codebase is functional but contains recurring technical debt patterns in four primary areas:
1. Defensive programming gaps around Visual Studio project context handling.
2. Widespread silent exception swallowing (`catch {}`) reducing diagnosability.
3. Duplicated service logic in label-generation domains.
4. Long, mixed-responsibility service classes that increase coupling and change risk.

### 中文
该代码库功能可用，但在四个主要方面存在重复出现的技术债务模式：
1. 对 Visual Studio 项目上下文的防御式编程不足。
2. 大量静默吞异常（`catch {}`）导致可诊断性差。
3. 标签生成领域存在重复服务逻辑。
4. 存在职责混杂的超长服务类，提升耦合与变更风险。

---

## 2) Technical Debt Inventory / 技术债务清单

| Category / 类别 | File / 文件 | Severity / 严重级别 | EN Finding / 英文问题描述 | 中文问题描述 |
|---|---|---|---|---|
| Missing validation / 校验缺失 | `HMT/Kernel/HMTProjectService.cs` | High | Several project access paths historically assume non-null active project context and `ProjectItems`. | 多个项目访问路径历史上默认活动项目上下文与 `ProjectItems` 非空，存在空引用风险。 |
| Exception handling gap / 异常处理缺陷 | `HMT/Kernel/HMTUtils.cs` | High | Many empty `catch` blocks suppress root-cause visibility. | 大量空 `catch` 块吞掉异常，根因不可见。 |
| Exception handling gap / 异常处理缺陷 | `HMT/Kernel/HMTDynamicsProcessor.cs` | High | XML processing swallows all failures and yields no signal for callers. | XML 处理吞掉所有错误，调用方无法感知失败。 |
| Exception handling gap / 异常处理缺陷 | `HMT/Kernel/HMTPasteText.cs` | Medium | Runtime code model probing catches without logging or fallback diagnostics. | 代码模型探测异常被吞掉，缺少日志与降级诊断。 |
| Dead code / 未实现代码 | `HMT/Kernel/CreateXppItem.cs` | Medium | `DoStrategyWork()` is unimplemented and always throws `NotImplementedException`. | `DoStrategyWork()` 未实现，始终抛 `NotImplementedException`。 |
| Code duplication / 代码重复 | `HMT/Services/Global/HMLabelService.cs`, `HMT/Services/Global/HMTLabelService.cs` | High | Two parallel label-service abstractions contain highly similar logic and factory patterns. | 两套并行标签服务抽象高度相似，存在逻辑重复和漂移风险。 |
| Separation of concerns / 职责分离不足 | `HMT/Services/Global/HMTBatchJobGenerateService.cs` | Medium | Service mixes VS project access, model interaction, and large template construction. | 服务同时承担 VS 项目访问、模型交互与大段模板拼装，职责混杂。 |
| Async robustness / 异步鲁棒性 | `HMT/Commands/WindowCommands/HMTJsonToDataContractWindowCommand.cs` | Low | `async void` command handler has no local exception handling. | `async void` 命令处理器缺少本地异常处理。 |
| Hardcoded configuration / 硬编码配置 | `HMT/Kernel/HMTUtils.cs` | Medium | Registry path traversal is hardcoded and lacks graceful fallback strategy. | 注册表路径硬编码，且缺乏优雅降级策略。 |

---

## 3) Prioritized Refactoring Themes / 优先级重构主题

### EN
1. **P1 (High impact, low risk):** Defensive null/context guards in project-navigation utilities.
2. **P1 (High impact, medium risk):** Replace silent catch blocks with structured logging and targeted exception handling.
3. **P2 (High impact, medium/high effort):** Consolidate duplicated label service abstractions.
4. **P2 (Medium impact):** Split large generators by concern (context access vs template generation).
5. **P3 (Medium impact):** Externalize environment/config assumptions and add validation fallbacks.

### 中文
1. **P1（高收益、低风险）**：在项目导航工具中补齐空值/上下文防御。
2. **P1（高收益、中风险）**：将静默吞异常改为结构化日志 + 精准异常处理。
3. **P2（高收益、中高工作量）**：合并重复的标签服务抽象层。
4. **P2（中收益）**：按职责拆分大型生成器（上下文访问 vs 模板生成）。
5. **P3（中收益）**：外置环境/配置假设并补齐校验与降级处理。

---

## 4) Traceability Notes / 可追溯说明

### EN
This report is based on static inspection of key modules and known maintenance hotspots from recent refactoring context.

### 中文
本报告基于关键模块的静态审查，以及近期重构上下文中已识别的维护热点。

