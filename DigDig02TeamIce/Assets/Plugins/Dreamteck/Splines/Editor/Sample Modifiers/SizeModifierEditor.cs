namespace Dreamteck.Splines.Editor
{
    using UnityEditor;
    using UnityEngine;

    public class SizeModifierEditor : SplineSampleModifierEditor
    {
        public bool allowSelection = true;
        private float addTime = 0f;

        public SizeModifierEditor(SplineUser user, SplineUserEditor editor) : base(user, editor, "_sizeModifier")
        {
            title = "Size Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add New Size"))
            {
                AddKey(addTime - 0.1f, addTime + 0.1f);
                UpdateValues();
            }
        }

        protected override void KeyGUI(SerializedProperty key)
        {
            SerializedProperty size = key.FindPropertyRelative("size");
            base.KeyGUI(key);
            EditorGUILayout.PropertyField(size);
        }
    }
}
