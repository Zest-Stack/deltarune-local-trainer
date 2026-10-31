using System;
using System.IO;
using System.Linq;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Compiler;
EnsureDataLoaded();
var folder = Path.GetFileName(Path.GetDirectoryName(FilePath));
int chapter = int.Parse(folder.Substring(7,1));
var context = new GlobalDecompileContext(Data);
string GML(UndertaleCode c) => new Underanalyzer.Decompiler.DecompileContext(context,c,Data.ToolInfo.DecompilerSettings).DecompileToString();
CodeImportGroup group = new(Data) { MainThreadAction = MainThreadAction };
var time = Data.Code.ByName("gml_Object_obj_time_Step_1");
if (time == null || GML(time).Contains("cdt_poll")) throw new Exception("Missing time event or already patched");
group.QueueAppend(time, File.ReadAllText(@"C:\Users\Administrator\Documents\Codex\2026-10-31\yio\work\trainer_step.gml").Replace("CHAPTER", chapter.ToString()));
string[] names = { "scr_damage", "scr_damage_all", "scr_damage_all_overworld", "scr_damage_proportional", "scr_damage_sneo_final_attack" };
foreach (string name in names)
{
    var c = Data.Code.ByName("gml_GlobalScript_"+name);
    if (c == null) continue;
    string src = GML(c);
    string guard = "\n    if (variable_global_exists(\"cdt_god\") && (global.cdt_god || global.cdt_lockhp)) return;\n";
    int at = src.IndexOf('{');
    if (at < 0 || !src.StartsWith("function "+name+"(")) throw new Exception("Unexpected damage script: "+name);
    group.QueueReplace(c,src.Insert(at+1,guard));
}
group.Import();
Directory.CreateDirectory(@"C:\Users\Administrator\Documents\Codex\2026-10-31\yio\work\patched");
File.WriteAllText(@"C:\Users\Administrator\Documents\Codex\2026-10-31\yio\work\patched\chapter"+chapter+".ok", "compiled");
