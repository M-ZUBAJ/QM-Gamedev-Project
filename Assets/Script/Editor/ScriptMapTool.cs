/*
First Date: 7/10/2026
Date: 8/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Script Map > Update Now.
              Tools > Script Map > Auto Update (ticked = runs by itself every time Unity finishes compiling scripts).
              Results: ScriptMap.md in the project folder (next to Assets), and warnings in the Console.
Purpose: Keeps the Requirement lines of every script in Assets/Script up to date.
         - Reads what each script says it needs (x->, <->, o->).
         - Searches the code to find which scripts really use which.
         - Writes the "<-x" / "<-o" lines (which scripts need this one) into each header.
           Don't write those two by hand, they are replaced every update.
         - Writes ScriptMap.md: every script, what it needs and what needs it.
         - Warns when a Requirement line doesn't match the code.
         It never changes the Date: line (that's for changes people make).
Requirement: ScriptMapTool.cs x-> MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ScriptMapTool
{
    // ===== Fields =====
    const string ScriptFolder = "Assets/Script";
    const string MapFileName = "ScriptMap.md";
    const string AutoKey = "ScriptMapTool.AutoUpdate";   // remembers if Auto Update is ticked
    const string MenuUpdate = "Tools/Script Map/Update Now";
    const string MenuAuto = "Tools/Script Map/Auto Update";

    const string FieldName = "Requirement:";
    const string Field = "Requirement: ";
    static readonly string Indent = new string(' ', Field.Length);   // lines under the first one line up with it

    // People write x-> <-> o->. This tool writes <-x <-o.
    static readonly string[] ToolArrows = { "<-x", "<-o" };

    // "A.cs x-> B.cs, C.cs"  ->  name = A.cs, arrow = x->, targets = B.cs, C.cs
    static readonly Regex LinkLine = new Regex(@"^(\S+)\s+(x->|<->|o->|<-x|<-o)\s+(.+)$");

    // Finds "class Name", "enum Name", ... so we know which names each file makes.
    static readonly Regex TypeName = new Regex(@"\b(namespace|class|struct|enum|interface|record)\s+([A-Za-z_][\w.]*)");

    // One link from a Requirement line, e.g. "x-> GameRules.cs".
    class Link
    {
        public string arrow;    // "x->", "<->" or "o->"
        public string target;   // e.g. "GameRules.cs" or "Light (Unity)"
        public bool IsUnity => target.EndsWith("(Unity)");
    }

    // One .cs file and what we found out about it.
    class Script
    {
        public string path;     // e.g. Assets/Script/Core/GameRules.cs
        public string name;     // e.g. GameRules.cs
        public string text;     // the whole file
        public string code;     // the file without comments and strings (so words in comments don't count)
        public bool bom;        // keep the file's encoding the same when we save it
        public bool hasHeader;
        public readonly List<string> types = new List<string>();        // classes / enums / ... made in this file
        public readonly List<string> handLines = new List<string>();    // Requirement lines people wrote, kept as they are
        public readonly List<Link> links = new List<Link>();
        public readonly HashSet<Script> uses = new HashSet<Script>();   // scripts this one uses in its code
    }

    // ===== Menu and Unity callbacks (Unity calls these) =====

    [MenuItem(MenuUpdate, false, MenuOrder.ScriptMapUpdateNow)]
    static void UpdateNow() => Run(true);

    [MenuItem(MenuAuto, false, MenuOrder.ScriptMapAutoUpdate)]
    static void ToggleAuto() => EditorPrefs.SetBool(AutoKey, !AutoUpdate);

    // Unity calls this to draw the tick next to "Auto Update".
    [MenuItem(MenuAuto, true)]
    static bool ToggleAutoTick()
    {
        Menu.SetChecked(MenuAuto, AutoUpdate);
        return true;
    }

    static bool AutoUpdate => EditorPrefs.GetBool(AutoKey, true);

    // Unity calls this every time it finishes compiling scripts.
    // If this run changes a header, Unity compiles again, this runs again, finds nothing to change and stops.
    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsCompiled()
    {
        if (!AutoUpdate) return;
        EditorApplication.delayCall += () => Run(false);   // wait until Unity is fully ready
    }

    // ===== My methods =====

    static void Run(bool fromMenu)
    {
        // Changing scripts while playing would stop the game, so wait.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (fromMenu) Debug.Log("Script Map: stop playing first.");
            return;
        }

        List<string> warnings = new List<string>();
        List<Script> scripts = LoadScripts(warnings);
        FindCodeUses(scripts);
        CheckRequirements(scripts, warnings);

        int changed = 0;
        foreach (Script s in scripts)
        {
            if (WriteHeader(s, scripts, warnings)) changed++;
        }
        bool mapChanged = WriteMap(scripts, warnings);

        if (changed > 0) AssetDatabase.Refresh();   // tell Unity the files changed

        string summary = $"Script Map: {scripts.Count} scripts, {changed} header(s) updated" +
                         (mapChanged ? ", ScriptMap.md updated." : ".");
        if (warnings.Count > 0)
            Debug.LogWarning(summary + " Warnings:\n" + string.Join("\n", warnings.Select(w => "- " + w)));
        else if (fromMenu || changed > 0 || mapChanged)
            Debug.Log(summary);
    }

    static List<Script> LoadScripts(List<string> warnings)
    {
        List<Script> scripts = new List<Script>();
        if (!Directory.Exists(ScriptFolder)) return scripts;

        foreach (string file in Directory.GetFiles(ScriptFolder, "*.cs", SearchOption.AllDirectories))
        {
            byte[] bytes = File.ReadAllBytes(file);
            Script s = new Script
            {
                path = file.Replace('\\', '/'),
                name = Path.GetFileName(file),
                text = File.ReadAllText(file),
                bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            };
            s.code = CodeOnly(s.text);
            FindTypes(s);
            ReadRequirement(s, warnings);
            scripts.Add(s);
        }

        // Requirement lines use file names, so two files with the same name would be mixed up.
        foreach (var group in scripts.GroupBy(s => s.name).Where(g => g.Count() > 1))
            warnings.Add($"There is more than one {group.Key}. Give each script its own name.");

        return scripts.OrderBy(s => s.path).ToList();   // same order every time, so ScriptMap.md only changes when something real changes
    }

    // Reads the Requirement lines people wrote in the header.
    static void ReadRequirement(Script s, List<string> warnings)
    {
        if (!TryGetHeader(s.text, out int start, out int end))
        {
            warnings.Add($"{s.name} has no /* */ header at the top.");
            return;
        }
        s.hasHeader = true;

        string[] lines = HeaderLines(s.text, start, end);
        if (!FindRequirement(lines, out int first, out int last)) return;   // WriteHeader adds one

        for (int i = first; i <= last; i++)
        {
            string line = (i == first ? lines[i].Substring(FieldName.Length) : lines[i]).Trim();
            if (line == "" || line == "None") continue;   // None is written again if nothing else is left

            Match m = LinkLine.Match(line);
            if (!m.Success)
            {
                warnings.Add($"{s.name}: can't read this Requirement line: \"{line}\"");
                s.handLines.Add(line);   // keep it, so nothing is lost
                continue;
            }

            string arrow = m.Groups[2].Value;
            if (ToolArrows.Contains(arrow)) continue;   // made by this tool, written again fresh

            if (m.Groups[1].Value != s.name)
                warnings.Add($"{s.name}: a Requirement line starts with {m.Groups[1].Value}, it should start with {s.name}.");

            s.handLines.Add(line);
            foreach (string part in m.Groups[3].Value.Split(','))
            {
                string target = part.Trim();
                if (target != "") s.links.Add(new Link { arrow = arrow, target = target });
            }
        }
    }

    // The header is the /* */ comment at the very top of the file.
    // start = where "/*" is, end = where "*/" is.
    static bool TryGetHeader(string text, out int start, out int end)
    {
        start = 0;
        end = 0;
        int i = 0;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
        if (i + 1 >= text.Length || text[i] != '/' || text[i + 1] != '*') return false;

        int close = text.IndexOf("*/", i + 2);
        if (close < 0) return false;

        start = i;
        end = close;
        return true;
    }

    static string[] HeaderLines(string text, int start, int end)
    {
        return text.Substring(start, end - start).Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
    }

    // Finds the "Requirement:" line and the indented lines under it.
    static bool FindRequirement(string[] lines, out int first, out int last)
    {
        first = System.Array.FindIndex(lines, l => l.StartsWith(FieldName));
        last = first;
        if (first < 0) return false;

        while (last + 1 < lines.Length && lines[last + 1].Trim() != "" && char.IsWhiteSpace(lines[last + 1][0]))
            last++;
        return true;
    }

    // Writes the new Requirement lines into the file. Returns true if the file changed.
    static bool WriteHeader(Script s, List<Script> scripts, List<string> warnings)
    {
        if (!s.hasHeader) return false;

        TryGetHeader(s.text, out int start, out int end);
        List<string> lines = HeaderLines(s.text, start, end).ToList();

        List<string> requirement = new List<string>(s.handLines);
        requirement.AddRange(ToolLines(s, scripts));
        if (requirement.Count == 0) requirement.Add("None");
        List<string> block = requirement.Select((line, i) => (i == 0 ? Field : Indent) + line).ToList();

        if (FindRequirement(lines.ToArray(), out int first, out int last))
        {
            lines.RemoveRange(first, last - first + 1);
            lines.InsertRange(first, block);
        }
        else if (lines[lines.Count - 1].Trim() == "")   // "*/" is on its own line: add Requirement just above it
        {
            lines.InsertRange(lines.Count - 1, block);
        }
        else
        {
            warnings.Add($"{s.name} has no Requirement: line, and \"*/\" isn't on its own line, so one couldn't be added.");
            return false;
        }

        string newline = s.text.Contains("\r\n") ? "\r\n" : "\n";   // keep the file's own line endings
        string newText = s.text.Substring(0, start) + string.Join(newline, lines) + s.text.Substring(end);
        if (newText == s.text) return false;

        File.WriteAllText(s.path, newText, new UTF8Encoding(s.bom));
        s.text = newText;
        return true;
    }

    // The "<-x" / "<-o" lines: which scripts need this one.
    static List<string> ToolLines(Script s, List<Script> scripts)
    {
        List<(Script other, string arrow)> neededBy = NeededBy(s, scripts);
        List<string> lines = new List<string>();

        string needs = string.Join(", ", neededBy.Where(n => n.arrow == "<-x").Select(n => n.other.name));
        string optional = string.Join(", ", neededBy.Where(n => n.arrow == "<-o").Select(n => n.other.name));
        if (needs != "") lines.Add($"{s.name} <-x {needs}");
        if (optional != "") lines.Add($"{s.name} <-o {optional}");
        return lines;
    }

    // Every script that needs this one, and how: "<-x" (needs), "<-o" (optional) or "<->" (each other).
    static List<(Script other, string arrow)> NeededBy(Script s, List<Script> scripts)
    {
        List<(Script, string)> result = new List<(Script, string)>();
        foreach (Script other in scripts.OrderBy(o => o.name))
        {
            if (other == s) continue;

            Link link = other.links.FirstOrDefault(l => l.target == s.name);
            bool eachOther = link?.arrow == "<->" || s.links.Any(l => l.target == other.name && l.arrow == "<->");

            if (eachOther) result.Add((other, "<->"));                               // already written by hand as <->
            else if (link != null) result.Add((other, link.arrow == "o->" ? "<-o" : "<-x"));
            else if (other.uses.Contains(s)) result.Add((other, "<-x"));             // used in code but not listed (also a warning)
        }
        return result;
    }

    // A script "uses" another when its code says one of the names the other file makes (e.g. "GameRules").
    static void FindCodeUses(List<Script> scripts)
    {
        foreach (Script user in scripts)
        {
            foreach (Script owner in scripts)
            {
                if (owner == user) continue;
                foreach (string type in owner.types)
                {
                    if (user.types.Contains(type)) continue;   // the user has its own thing with this name
                    if (Regex.IsMatch(user.code, $@"\b{Regex.Escape(type)}\b"))
                    {
                        user.uses.Add(owner);
                        break;
                    }
                }
            }
        }
    }

    // Only the outside names count (class Blueprint), not the ones inside (Blueprint's class Part),
    // because short inside names like "Part" would match all kinds of other code.
    static void FindTypes(Script s)
    {
        HashSet<int> outsideDepths = new HashSet<int> { 0 };   // brace depths where an outside name can be
        int depth = 0;
        int pos = 0;

        foreach (Match m in TypeName.Matches(s.code))
        {
            for (; pos < m.Index; pos++)
            {
                if (s.code[pos] == '{') depth++;
                else if (s.code[pos] == '}') depth--;
            }

            if (m.Groups[1].Value == "namespace")
            {
                // "namespace X { ... }" puts its names one brace deeper. "namespace X;" doesn't.
                int next = m.Index + m.Length;
                while (next < s.code.Length && char.IsWhiteSpace(s.code[next])) next++;
                if (next < s.code.Length && s.code[next] == '{') outsideDepths.Add(depth + 1);
                continue;
            }

            if (outsideDepths.Contains(depth)) s.types.Add(m.Groups[2].Value);
        }
    }

    // The code with comments and "strings" blanked out.
    static string CodeOnly(string text)
    {
        StringBuilder sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (c == '/' && next == '/')        // comment to the end of the line
            {
                while (i < text.Length && text[i] != '\n') i++;
            }
            else if (c == '/' && next == '*')   // /* comment */
            {
                int close = text.IndexOf("*/", i + 2);
                i = close < 0 ? text.Length : close + 2;
                sb.Append(' ');
            }
            else if (c == '"' || c == '\'')     // "string" or 'c'
            {
                bool verbatim = c == '"' && i > 0 && text[i - 1] == '@';   // @"..." has no \ escapes
                i++;
                while (i < text.Length)
                {
                    if (verbatim)
                    {
                        if (text[i] == '"')
                        {
                            if (i + 1 < text.Length && text[i + 1] == '"') { i += 2; continue; }   // "" inside @"..."
                            break;
                        }
                    }
                    else
                    {
                        if (text[i] == '\\') { i += 2; continue; }
                        if (text[i] == c || text[i] == '\n') break;
                    }
                    i++;
                }
                i++;
                sb.Append(' ');
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }

    // Warns when what a header says doesn't match the code.
    static void CheckRequirements(List<Script> scripts, List<string> warnings)
    {
        foreach (Script s in scripts)
        {
            if (!s.hasHeader) continue;

            foreach (Script used in s.uses.OrderBy(u => u.name))
            {
                bool listed = s.links.Any(l => l.target == used.name);
                bool otherSaysEachOther = used.links.Any(l => l.target == s.name && l.arrow == "<->");   // warned about below instead
                if (!listed && !otherSaysEachOther)
                    warnings.Add($"{s.name} uses {used.name} in its code, but its Requirement doesn't list it. Add: {s.name} x-> {used.name}");
            }

            foreach (Link l in s.links)
            {
                if (l.IsUnity) continue;

                Script target = scripts.FirstOrDefault(o => o.name == l.target);
                if (target == null)
                {
                    warnings.Add($"{s.name} lists {l.target}, but there is no script with that name in {ScriptFolder}.");
                    continue;
                }
                if (!s.uses.Contains(target))
                    warnings.Add($"{s.name} lists {l.target}, but its code doesn't use it.");
                if (l.arrow == "<->" && !target.links.Any(t => t.target == s.name && t.arrow == "<->"))
                    warnings.Add($"{s.name} says {s.name} <-> {target.name}, but {target.name} doesn't say it back. Add: {target.name} <-> {s.name}");
            }
        }
    }

    // Writes ScriptMap.md next to the Assets folder. Returns true if it changed.
    static bool WriteMap(List<Script> scripts, List<string> warnings)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("# Script Map\n\n");
        sb.Append("Made by **Tools > Script Map** in Unity. Don't edit this file by hand, it is rewritten every update.\n\n");

        sb.Append("## Warnings\n\n");
        if (warnings.Count == 0) sb.Append("None.\n");
        foreach (string w in warnings) sb.Append("- ").Append(w).Append('\n');

        sb.Append("\n## Scripts\n\n");
        sb.Append("| Script | Folder | Needs | Needed by |\n");
        sb.Append("|---|---|---|---|\n");
        foreach (Script s in scripts)
        {
            string needs = JoinOrDash(s.links.Select(l => l.target + Tag(l.arrow)));
            string neededBy = JoinOrDash(NeededBy(s, scripts).Select(n => n.other.name + Tag(n.arrow)));
            sb.Append($"| [{s.name}](<{s.path}>) | {Folder(s)} | {needs} | {neededBy} |\n");
        }

        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, MapFileName);
        string text = sb.ToString();
        if (File.Exists(path) && File.ReadAllText(path) == text) return false;

        File.WriteAllText(path, text, new UTF8Encoding(false));
        return true;
    }

    static string Tag(string arrow)
    {
        if (arrow == "o->" || arrow == "<-o") return " (optional)";
        if (arrow == "<->") return " (each other)";
        return "";
    }

    static string JoinOrDash(IEnumerable<string> items)
    {
        string joined = string.Join(", ", items);
        return joined == "" ? "-" : joined;
    }

    // e.g. "Core" for Assets/Script/Core/GameRules.cs
    static string Folder(Script s)
    {
        string dir = Path.GetDirectoryName(s.path).Replace('\\', '/');
        return dir == ScriptFolder ? "." : dir.Substring(ScriptFolder.Length + 1);
    }
}
