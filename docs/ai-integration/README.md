# AI + VSIX SDK 集成指南

## 概述

HMT 支持通过 **AI 生成的 JSON 规范文件** 来创建 D365 F&O X++ 元数据对象。这种方式结合了 AI 的快速生成能力和 VSIX SDK 的类型安全保证，比直接用 LLM + grep 操作 XML 文件更加可靠。

## 工作流程

```
1. AI CLI 根据需求生成 JSON Spec 文件
2. 在 Visual Studio 中打开 D365 项目  
3. HMT 菜单 → Import AI Metadata Spec
4. 选择 JSON 文件 → 自动验证 → 确认 → 创建对象
```

## JSON Spec 格式

### 顶层结构

```json
{
  "version": "1.0",
  "objects": [
    {
      "objectType": "Table | Form | SecurityPrivilege | MenuItem | Edt | Enum",
      "table": { ... },
      "form": { ... },
      "securityPrivilege": { ... },
      "menuItem": { ... },
      "edt": { ... },
      "enum": { ... }
    }
  ]
}
```

`objectType` 决定使用哪个子对象（table/form/...），其余子对象为 null。

### 对象类型

#### Enum

```json
{
  "objectType": "Enum",
  "enum": {
    "name": "VehicleType",
    "label": "Vehicle type",
    "helpText": "Specifies the type of vehicle",
    "values": [
      { "name": "Car", "label": "Car", "value": 0 },
      { "name": "Truck", "label": "Truck", "value": 1 }
    ],
    "createEdtType": true,
    "edtTypeName": "VehicleTypeId"
  }
}
```

#### EDT

```json
{
  "objectType": "Edt",
  "edt": {
    "name": "VehicleId",
    "type": "String",
    "extends": "SysGroup",
    "label": "Vehicle ID",
    "helpText": "Unique identifier",
    "stringSize": 20,
    "referenceTable": "VehicleTable",
    "referenceField": "VehicleId",
    "enumType": ""
  }
}
```

**type**: `String | Int | Int64 | Real | Date | DateTime | Enum | Guid | Container`

#### Table

```json
{
  "objectType": "Table",
  "table": {
    "name": "VehicleTable",
    "label": "Vehicles",
    "tableGroup": "Main",
    "cacheLevel": "Found",
    "titleField1": "VehicleId",
    "fields": [
      {
        "name": "VehicleId",
        "type": "String",
        "edt": "VehicleId",
        "mandatory": true,
        "allowEdit": false
      }
    ],
    "indexes": [
      {
        "name": "VehicleIdIdx",
        "fields": ["VehicleId"],
        "alternateKey": true
      }
    ],
    "fieldGroups": [
      {
        "name": "Overview",
        "label": "Overview",
        "fields": ["VehicleId", "Description"]
      }
    ],
    "relations": [
      {
        "name": "VehicleTypeRelation",
        "relatedTable": "VehicleTypeTable",
        "constraints": [
          { "field": "VehicleType", "relatedField": "VehicleType" }
        ]
      }
    ]
  }
}
```

**tableGroup**: `Main | Group | WorksheetHeader | WorksheetLine | Transaction | Miscellaneous | Parameter | Reference | Framework`  
**cacheLevel**: `None | NotInTTS | Found | FoundAndEmpty | EntireTable`

#### Form

```json
{
  "objectType": "Form",
  "form": {
    "name": "VehicleTable",
    "label": "Vehicles",
    "pattern": "SimpleList",
    "dataSource": "VehicleTable",
    "gridFields": ["VehicleId", "Description"],
    "detailsHeaderFields": ["VehicleId"],
    "tabPages": [
      {
        "name": "DetailsTabPage",
        "caption": "Details",
        "fields": ["Description", "VehicleType"]
      }
    ],
    "createMenuItem": true
  }
}
```

**pattern**: `SimpleList | SimpleListDetails`

#### MenuItem

```json
{
  "objectType": "MenuItem",
  "menuItem": {
    "name": "VehicleTable",
    "objectName": "VehicleTable",
    "type": "Display",
    "label": "Vehicles",
    "helpText": "Manage vehicles"
  }
}
```

**type**: `Display | Action | Output`

#### SecurityPrivilege

```json
{
  "objectType": "SecurityPrivilege",
  "securityPrivilege": {
    "name": "VehicleTableView",
    "label": "View vehicles",
    "accessLevel": "Read",
    "entryPoints": [
      {
        "name": "VehicleTable",
        "objectName": "VehicleTable",
        "objectType": "MenuItemDisplay",
        "forms": ["VehicleTable"]
      }
    ]
  }
}
```

**accessLevel**: `Read | Update | Create | Correct | Delete`

## 依赖顺序

对象会按以下顺序自动创建，确保依赖关系正确：

1. **Enum** - 先创建枚举
2. **EDT** - 创建扩展数据类型
3. **Table** - 创建表（引用 EDT 和 Enum）
4. **Form** - 创建窗体（引用 Table）
5. **MenuItem** - 创建菜单项（引用 Form）
6. **SecurityPrivilege** - 创建安全权限（引用 MenuItem）

## 验证

导入前会自动验证：
- 必填字段检查
- 名称冲突检测（对象是否已存在）
- EDT/Table/Enum 引用合法性
- Index 字段是否在表中定义
- 枚举值名称唯一性

验证结果分三级：
- **Error**: 阻断执行，必须修复
- **Warning**: 提示风险，可继续
- **Info**: 信息提示

## AI CLI Prompt 模板

在使用 AI CLI 生成 spec 时，可使用以下 prompt：

```
请根据以下需求生成一个 HMT AI Metadata Spec JSON 文件。

需求：[描述你的需求]

请严格遵循以下 JSON Schema 格式：
- 顶层包含 version("1.0") 和 objects 数组
- 每个 object 包含 objectType 和对应的子对象
- 对象按依赖顺序排列：Enum → EDT → Table → Form → MenuItem → SecurityPrivilege
- 所有名称使用 PascalCase
- 表字段必须指定 type 和 edt（如有）
- 为每个表创建至少一个带 alternateKey 的索引

输出纯 JSON，不要包含其他内容。
```
