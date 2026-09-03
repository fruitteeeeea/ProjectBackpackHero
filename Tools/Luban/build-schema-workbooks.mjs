import fs from "node:fs/promises";
import { fileURLToPath } from "node:url";

const outputDir = fileURLToPath(new URL("./schema/", import.meta.url));
const sourceTableDir = fileURLToPath(new URL("./tables/", import.meta.url));
const nativeTableDir = fileURLToPath(new URL("./native-tables/", import.meta.url));
await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(nativeTableDir, { recursive: true });

const tableColumns = [
  "id", "itemType", "name", "description", "unlockRequirementText", "cost", "debris",
  "upgradeFragmentsLevel1", "upgradeFragmentsLevel9", "aircraftHealthMultiplierLevel1",
  "aircraftHealthMultiplierLevel10", "aircraftDamageMultiplierLevel1", "aircraftDamageMultiplierLevel10",
  "equipmentIntervalMultiplierLevel1", "equipmentIntervalMultiplierLevel10", "cooldown", "count",
  "equipmentItemModifier", "applyEquipmentItemModifierToProjectileStats", "price",
  "aircraftCooldownReductionLevel2", "aircraftCooldownReductionLevel3", "equipmentEffectIntervalReductionLevel2",
  "equipmentEffectIntervalReductionLevel3", "fighterId", "shapeId",
];

const fieldTypes = new Map([
  ["id", "string"], ["itemType", "int"], ["name", "string"], ["description", "string"],
  ["unlockRequirementText", "string"], ["cost", "int"], ["debris", "int"],
  ["upgradeFragmentsLevel1", "int"], ["upgradeFragmentsLevel9", "int"],
  ["aircraftHealthMultiplierLevel1", "float"], ["aircraftHealthMultiplierLevel10", "float"],
  ["aircraftDamageMultiplierLevel1", "float"], ["aircraftDamageMultiplierLevel10", "float"],
  ["equipmentIntervalMultiplierLevel1", "float"], ["equipmentIntervalMultiplierLevel10", "float"],
  ["cooldown", "float"], ["count", "int"], ["equipmentItemModifier", "float"],
  ["applyEquipmentItemModifierToProjectileStats", "bool"], ["price", "int"],
  ["aircraftCooldownReductionLevel2", "float"], ["aircraftCooldownReductionLevel3", "float"],
  ["equipmentEffectIntervalReductionLevel2", "float"], ["equipmentEffectIntervalReductionLevel3", "float"],
  ["fighterId", "string"], ["shapeId", "string"],
]);

const fighterColumns = ["id", "displayName", "baseSpeed", "maximumHealth", "attackRange", "targetingArcAngle", "targetingPriority", "targetingMode", "attackInterval", "projectileDamage", "projectileSpeed", "projectileLifetimeOverride"];
const fighterTypes = ["string", "string", "float", "int", "float", "float", "int", "int", "float", "float", "float", "float"];
const levelColumns = ["playerAircraftHealthMultiplier", "playerAircraftDamageMultiplier", "backpackRoundHealthMultipliers", "enemyStages"];
const levelTypes = ["float", "float", "string", "string"];

function toCsv(rows) {
  return rows.map((row) => row.map((value) => {
    const text = value == null ? "" : String(value);
    return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
  }).join(',')).join('\r\n') + '\r\n';
}

function parseCsv(text) {
  const rows = [];
  let row = [];
  let value = "";
  let quoted = false;
  for (let index = 0; index < text.length; index += 1) {
    const char = text[index];
    if (quoted) {
      if (char === '"' && text[index + 1] === '"') {
        value += char;
        index += 1;
      } else if (char === '"') {
        quoted = false;
      } else {
        value += char;
      }
    } else if (char === '"') {
      quoted = true;
    } else if (char === ',') {
      row.push(value);
      value = "";
    } else if (char === '\n') {
      if (value.endsWith('\r')) value = value.slice(0, -1);
      row.push(value);
      rows.push(row);
      row = [];
      value = "";
    } else {
      value += char;
    }
  }
  if (value.length > 0 || row.length > 0) {
    row.push(value);
    rows.push(row);
  }
  return rows;
}

async function buildNativeInputCsvs() {
  for (const fileName of ["ItemConfig.csv", "FighterConfig.csv", "LevelConfig.csv"]) {
    const sourceRows = parseCsv(await fs.readFile(`${sourceTableDir}${fileName}`, "utf8"));
    if (sourceRows.length < 2) throw new Error(`${fileName} must contain a header and at least one data row.`);
    const nativeRows = [
      ["##", ...sourceRows[0]],
      Array(sourceRows[0].length + 1).fill(null),
      ...sourceRows.slice(1).map((row) => [null, ...row]),
    ];
    await fs.writeFile(`${nativeTableDir}${fileName}`, toCsv(nativeRows), "utf8");
  }
}

async function saveTables() {
  const rows = [
    ["##var", "full_name", "value_type", "read_schema_from_file", "input", "index", "mode", "group", "comment", "tags", "output"],
    ["##", "全名(包含模块和名字)", "记录类名", "从 Excel 读取定义", "文件列表", "表 id 字段", "模式", "分组", "注释", null, "输出文件名"],
    ["##", null, null, "false 时采用 __beans__ 定义", "CSV 文件名", "空则自动取首字段", "one|map|list", "c", null, null, null],
    [null, "BackpackHero.Config.ItemConfigs", "ItemConfig", false, "ItemConfig.csv", "id", "map", "c", "Backpack item configuration", null, null],
    [null, "BackpackHero.Config.FighterConfigs", "FighterConfig", false, "FighterConfig.csv", "id", "map", "c", "Fighter gameplay configuration", null, null],
    [null, "BackpackHero.Config.LevelConfigs", "LevelConfig", false, "LevelConfig.csv", null, "one", "c", "Level difficulty configuration", null, null],
  ];
  await fs.writeFile(`${outputDir}__tables__.csv`, toCsv(rows), 'utf8');
}

function beanRows(fullName, columns, types) {
  return columns.map((name, index) => [
    null,
    index === 0 ? fullName : null,
    null, null, null, null, null, null, index === 0 ? "c" : null,
    name, null, Array.isArray(types) ? types[index] : types.get(name), null, null, null, null,
  ]);
}

async function saveBeans() {
  const rows = [
    ["##var", "full_name", "parent", "valueType", "sep", "alias", "comment", "tags", "group", "[*fields", null, null, null, null, null, "*fields]"],
    ["##var", null, null, null, null, null, null, null, null, "name", "alias", "type", "group", "comment", "tags", "variants"],
    ["##", "全名(包含模块和名字)", "父类", "是否值类型", "分割符", "别名", null, null, null, "字段名", "字段别名", "类型", "分组", "注释", null, null],
    ...beanRows("BackpackHero.Config.ItemConfig", tableColumns, fieldTypes),
    [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null],
    ...beanRows("BackpackHero.Config.FighterConfig", fighterColumns, fighterTypes),
    [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null],
    ...beanRows("BackpackHero.Config.LevelConfig", levelColumns, levelTypes),
  ];
  await fs.writeFile(`${outputDir}__beans__.csv`, toCsv(rows), 'utf8');
}

await saveTables();
await saveBeans();
await buildNativeInputCsvs();
