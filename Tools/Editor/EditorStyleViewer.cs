using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SharedTools {
    public sealed class EditorStyleViewer : EditorWindow {

        private string search = string.Empty;
        private Vector2 scroll = Vector2.zero;
        private GUIStyle selectedStyle;

        [MenuItem("Window/Editor Styles Viewer")]
        private static void Init() {
            var window = GetWindow<EditorStyleViewer>();

            window.titleContent = new GUIContent("Styles Viewer");
            window.Show();
        }

        private void OnGUI() {
            EditorGUILayout.BeginHorizontal("toolbar");

            //GUI.enabled = InternalEditorUtility.HasPro();
            if (GUILayout.Button(EditorGUIUtility.isProSkin ? "White Skin" : "Dark Skin", "toolbarbutton"))
                InternalEditorUtility.SwitchSkinAndRepaintAllViews();
            GUI.enabled = true;

            if (GUILayout.Button("Generate Styles Class", "toolbarbutton"))
                GenerateClassFile();

            GUILayout.FlexibleSpace();

            search = SearchBar(search);

            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll, false, false);

            for (var i = 0; i < GUI.skin.customStyles.Length; i++) {
                var style = GUI.skin.customStyles[i];

                if (style.name.ToLower().Contains(search.ToLower()))
                    DrawStyle(style);
            }
            EditorGUILayout.EndScrollView();
        }

        private string GetHighlightedName(GUIStyle style) {
            var name = style.name;

            if (string.IsNullOrEmpty(search) || !name.ToLower().Contains(search.ToLower()))
                return name;

            var startIndex = name.ToLower().IndexOf(search.ToLower());
            var endIndex = startIndex + search.Length;

            return name.Insert(endIndex, "</b>").Insert(startIndex, "<b>");
        }

        private void DrawStyle(GUIStyle style) {
            EditorGUILayout.BeginHorizontal("EyedropperHorizontalLine");
            if (GUILayout.Button(style.name, style))
                EditorGUIUtility.systemCopyBuffer = style.name;
            GUILayout.FlexibleSpace();
            EditorGUILayout.SelectableLabel(GetHighlightedName(style), "IN Label");
            EditorGUILayout.EndHorizontal();
        }

        private static string SearchBar(string search, params GUILayoutOption[] options) {
            var paramsTypes = new [] { typeof(string), typeof(GUILayoutOption[]) };
            var methodName = "ToolbarSearchField";
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var type = typeof(EditorGUILayout);
            var parameters = new object[] { search, options };
            var method = type.GetMethod(methodName, flags, null, paramsTypes, null);
            var result = method.Invoke(null, parameters);
            return (string)result;
        }

        public static void GenerateClassFile() {
            var path = EditorUtility.SaveFilePanel("Save Styles Class", "Assets", "Styles", "cs");

            if (path.Length == 0)
                return;

            if (File.Exists(path))
                File.Delete(path);

            try {
                using(var file = new StreamWriter(path)) {
                    file.WriteLine("using UnityEngine;");
                    file.WriteLine("");
                    file.WriteLine("//Automatic Generated;");
                    file.WriteLine("#if UNITY_EDITOR");
                    file.WriteLine("public static class CustomStyles {");
                    file.WriteLine("\tpublic static GUIStyle[] allStyles { get { return GUI.skin.customStyles; } }");
                    file.WriteLine("");

                    var line = "\tpublic static GUIStyle {0} {{ get {{ return \"{1}\"; }} }}";
                    //var line = "\tpublic static GUIStyle {0} {{ get {{ return new GUIStyle(\"{1}\"); }} }}";
                    //var line = "\tpublic static GUIStyle {0} = \"{1}\";";

                    foreach (var style in GUI.skin.customStyles) {
                        var chars = style.name.Replace(".", "")
                            .Replace(",", "")
                            .Replace(" ", "")
                            .Replace("_", "").ToCharArray();

                        if (char.ToUpper(chars[1]) != chars[1])
                            chars[0] = char.ToLower(chars[0]);

                        file.WriteLine(line, new string(chars), style.name);
                    }

                    file.WriteLine("}");
                    file.WriteLine("#endif");
                }
            } catch (Exception e) {
                if (File.Exists(path))
                    File.Delete(path);

                Debug.LogException(e);
            }

            AssetDatabase.Refresh();
        }
    }
}
