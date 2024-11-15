#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace UVExporter.Editor
{
    public class UVExporter : EditorWindow
    {
        private Renderer _targetRenderer;
        private Texture2D _uvTexture;
        private int _resolutionIndex = 2;
        private readonly string[] _resolutionOptions = { "512", "1024", "2048", "4096" };
        private Color _backgroundColor = Color.white;
        private Color _uvLineColor = Color.black;
        private bool _isTransparent = false;
        private string _lastSavedPath = "Assets";
    
        private bool _isFillTriangleEnabled = false;
        private bool _autoTriangleColor = false;
        private Color _triangleColor = Color.gray;
        private bool _isDrawLineEnabled = true;
    
        private readonly string[] _languageOptions = { "English", "Japanese" };
        private int _languageIndex = 0;

        [MenuItem("Tools/UV Exporter")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(UVExporter), false, "UV Exporter");
            window.minSize = new Vector2(300, 330);
            window.maxSize = window.minSize;
        
        }
    
        private void OnEnable()
        {
            string savedLanguage = EditorPrefs.GetString("UVExporterLanguage", "English");
            _languageIndex = Array.IndexOf(_languageOptions, savedLanguage);
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            EditorGUILayout.LabelField("UV Exporter", style);
            EditorGUILayout.Space();

            _targetRenderer = EditorGUILayout.ObjectField(_languageOptions[_languageIndex] == "Japanese" ? "メッシュ" : "Target Mesh", _targetRenderer, typeof(Renderer), true) as Renderer;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        
            _resolutionIndex = EditorGUILayout.Popup(_languageOptions[_languageIndex] == "Japanese" ? "解像度" : "Resolution", _resolutionIndex, _resolutionOptions);
        
            EditorGUILayout.Space();
        
            _isTransparent = EditorGUILayout.Toggle(_languageOptions[_languageIndex] == "Japanese" ? "背景を透過" : "Transparent Background", _isTransparent);
            using (new EditorGUI.DisabledScope(_isTransparent))
            {
                _backgroundColor = EditorGUILayout.ColorField(_languageOptions[_languageIndex] == "Japanese" ? "背景色" : "Background Color", _backgroundColor);
            }
            _uvLineColor = EditorGUILayout.ColorField(_languageOptions[_languageIndex] == "Japanese" ? "線の色" : "UV Line Color", _uvLineColor);
        
            EditorGUILayout.Space();
            _isFillTriangleEnabled = EditorGUILayout.Toggle(_languageOptions[_languageIndex] == "Japanese" ? "三角面を塗りつぶす" : "Enable Fill Triangle", _isFillTriangleEnabled);
        
            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!_isFillTriangleEnabled))
            {
                _autoTriangleColor = EditorGUILayout.Toggle(_languageOptions[_languageIndex] == "Japanese" ? "自動で三角面の色を決定" : "Auto Triangle Color", _autoTriangleColor);
                using (new EditorGUI.DisabledScope(_autoTriangleColor))
                {
                    _triangleColor = EditorGUILayout.ColorField(_languageOptions[_languageIndex] == "Japanese" ? "三角面の色" : "Triangle Color", _triangleColor);
                }
                _isDrawLineEnabled = EditorGUILayout.Toggle(_languageOptions[_languageIndex] == "Japanese" ? "線を描画する" : "Enable Draw Line", _isDrawLineEnabled);
            }
            EditorGUI.indentLevel--;
        
            EditorGUILayout.Space();
            if (GUILayout.Button("Export UV"))
            {
                if (_targetRenderer != null)
                {
                    ExportUV();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", _languageOptions[_languageIndex] == "Japanese" ? "メッシュが選択されていません" : "No mesh selected", "OK");
                }
            }
        
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;

            int newLanguageIndex = EditorGUILayout.Popup("Language", _languageIndex, _languageOptions);
            if (newLanguageIndex != _languageIndex)
            {
                _languageIndex = newLanguageIndex;
                EditorPrefs.SetString("UVExporterLanguage", _languageOptions[_languageIndex]);
            }
        }

        private void ExportUV()
        {
            Mesh mesh = null;
            if (_targetRenderer is MeshRenderer)
            {
                mesh = (_targetRenderer as MeshRenderer).gameObject.GetComponent<MeshFilter>().sharedMesh;
            }
            else if (_targetRenderer is SkinnedMeshRenderer)
            {
                mesh = (_targetRenderer as SkinnedMeshRenderer).sharedMesh;
            }

            if (mesh == null)
            {
                EditorUtility.DisplayDialog("Error", _languageOptions[_languageIndex] == "Japanese" ? "メッシュが選択されていません" : "No mesh selected", "OK");
                return;
            }
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;

            int textureSize = int.Parse(_resolutionOptions[_resolutionIndex]);
            Texture2D uvTexture = new Texture2D(textureSize, textureSize);
            Color[] pixels = new Color[textureSize * textureSize];

            // Set all pixels to white or transparent based on _isTransparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = _isTransparent ? new Color(_backgroundColor.r, _backgroundColor.g, _backgroundColor.b, 0) : _backgroundColor;
            }
        

            // Draw lines and fill triangles for each triangle
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector2 uv0 = uvs[triangles[i]];
                Vector2 uv1 = uvs[triangles[i + 1]];
                Vector2 uv2 = uvs[triangles[i + 2]];

                // Only call FillTriangle if _isFillTriangleEnabled is true
                if (_isFillTriangleEnabled)
                {
                    FillTriangle(uv0, uv1, uv2, pixels, textureSize);
                }
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector2 uv0 = uvs[triangles[i]];
                Vector2 uv1 = uvs[triangles[i + 1]];
                Vector2 uv2 = uvs[triangles[i + 2]];
            
            
            
                // Only call DrawLine if _isDrawLineEnabled is true or _isFillTriangleEnabled is false
                if (_isDrawLineEnabled || !_isFillTriangleEnabled)
                {
                    DrawLine(uv0, uv1, pixels, textureSize, _uvLineColor);
                    DrawLine(uv1, uv2, pixels, textureSize, _uvLineColor);
                    DrawLine(uv2, uv0, pixels, textureSize, _uvLineColor);
                }
            }
        
            uvTexture.SetPixels(pixels);
            uvTexture.Apply();

            // Save the texture as PNG
            byte[] bytes = uvTexture.EncodeToPNG();
            string defaultName = _targetRenderer.gameObject.name + "_uv.png";
        
            string path = EditorUtility.SaveFilePanel("Save UV Texture", _lastSavedPath, defaultName, "png");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, bytes);
                Debug.Log("Saved UV texture to: " + path);
                _lastSavedPath = System.IO.Path.GetDirectoryName(path);
            }
        
            AssetDatabase.Refresh();
        }

        private void FillTriangle(Vector2 uv0, Vector2 uv1, Vector2 uv2, Color[] pixels, int textureSize)
        {
            // Convert UV coordinates to pixel coordinates
            Vector2Int p0 = new Vector2Int(Mathf.FloorToInt(uv0.x * textureSize), Mathf.FloorToInt(uv0.y * textureSize));
            Vector2Int p1 = new Vector2Int(Mathf.FloorToInt(uv1.x * textureSize), Mathf.FloorToInt(uv1.y * textureSize));
            Vector2Int p2 = new Vector2Int(Mathf.FloorToInt(uv2.x * textureSize), Mathf.FloorToInt(uv2.y * textureSize));

            // Compute bounding box of the triangle
            int minX = Mathf.Min(p0.x, p1.x, p2.x);
            int maxX = Mathf.Max(p0.x, p1.x, p2.x);
            int minY = Mathf.Min(p0.y, p1.y, p2.y);
            int maxY = Mathf.Max(p0.y, p1.y, p2.y);

            // Compute the average position of the UV coordinates
            Vector2 uvAvg = (uv0 + uv1 + uv2) / 3f;

            // Convert the average position to a hue value
            float hue = uvAvg.magnitude % 1f;

            // Convert the hue to a color
            Color color = _autoTriangleColor ? Color.HSVToRGB(hue, 1f, 1f) : new Color(_triangleColor.r, _triangleColor.g, _triangleColor.b, 1f);

            // Rasterize the triangle
            Vector2Int p = new Vector2Int();
            for (p.y = minY; p.y <= maxY; p.y++)
            {
                for (p.x = minX; p.x <= maxX; p.x++)
                {
                    // Compute barycentric coordinates
                    float w0 = EdgeFunction(p1, p2, p);
                    float w1 = EdgeFunction(p2, p0, p);
                    float w2 = EdgeFunction(p0, p1, p);

                    // If p is on or inside all edges, render pixel
                    if (w0 >= 0 && w1 >= 0 && w2 >= 0)
                    {
                        pixels[p.y * textureSize + p.x] = color;
                    }
                }
            }
        }

        private float EdgeFunction(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
        }

        private void DrawLine(Vector2 pointA, Vector2 pointB, Color[] pixels, int textureSize, Color color)
        {
            int stepCount = (int)(Vector2.Distance(pointA, pointB) * 10000);

            for (int i = 0; i <= stepCount; i++)
            {
                float t = (float)i / stepCount;
                Vector2 point = Vector2.Lerp(pointA, pointB, t);
                int x = Mathf.Clamp(Mathf.FloorToInt(point.x * textureSize), 0, textureSize - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(point.y * textureSize), 0, textureSize - 1);
                pixels[y * textureSize + x] = color;
            }
        }
    }
}
#endif
