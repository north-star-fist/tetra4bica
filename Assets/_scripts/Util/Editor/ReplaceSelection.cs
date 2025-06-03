/* This wizard replaces a selection with an object or prefab.
 * Scene objects will be cloned (destroying their prefab links).
 * 
 * Original coding by 'yesfish', nabbed from Unity Forums
 * 'keep parent' added by Dave A (also removed 'rotation' option, using localRotation)
 */
using System.Collections.Generic;
using Sergey.Safonov.Utility;
using UnityEditor;
using UnityEngine;


public class ReplaceSelection : ScriptableWizard
{
    private static GameObject s_replacement = null;
    private static bool s_keep = false;
    private static bool s_noRotations;
    private static bool s_noScale;

    public GameObject ReplacementObject = null;
    public bool KeepOriginals = false;
    public bool NoRenaming;
    public bool NoRotation;
    public bool NoScaling;
    public bool InheritLayer;
    public bool InheritLayerRecursively;
    public bool InheritTag;
    public bool InheritStaticness;


    [MenuItem("Tools/Replace Selection...")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard(
            "Replace Selection", typeof(ReplaceSelection), "Replace");
    }


    public ReplaceSelection()
    {
        ReplacementObject = s_replacement;
        KeepOriginals = s_keep;
        NoRotation = s_noRotations;
        NoScaling = s_noScale;
    }


    void OnWizardUpdate()
    {
        s_replacement = ReplacementObject;
        s_keep = KeepOriginals;
        s_noRotations = NoRotation;
        s_noScale = NoScaling;
    }


    void OnWizardCreate()
    {
        if (s_replacement == null)
            return;

        Transform[] transforms = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);

        List<GameObject> saved = new List<GameObject>();
        foreach (var tr in transforms)
        {
            if (tr.parent != null)
            {
                if (!saved.Contains(tr.root.gameObject))
                {
                    Undo.RegisterFullObjectHierarchyUndo(tr.parent.gameObject, "Snapshot of " + tr.parent.gameObject);
                    saved.Add(tr.root.gameObject);
                }
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(tr.gameObject, "Saving original " + tr.name);
            }
        }

        foreach (Transform t in transforms)
        {
            GameObject newGo;
            PrefabType pref = PrefabUtility.GetPrefabType(s_replacement);

            if (pref == PrefabType.Prefab || pref == PrefabType.ModelPrefab)
            {
                newGo = (GameObject)PrefabUtility.InstantiatePrefab(s_replacement);
            }
            else
            {
                newGo = GameObject.Instantiate(s_replacement);
            }
            Undo.RegisterCreatedObjectUndo(newGo, newGo.name + " prefab object instantiating");
            Transform newGoTrans = newGo.transform;
            newGoTrans.parent = t.parent;
            newGo.name = NoRenaming ? t.name : s_replacement.name;
            newGoTrans.localPosition = t.localPosition;
            if (!s_noScale)
            {
                newGoTrans.localScale = t.localScale;
            }
            if (!s_noRotations)
            {
                newGoTrans.localRotation = t.localRotation;
            }
            if (InheritLayerRecursively)
            {
                GameObjectUtil.SetLayerRecursively(newGo, t.gameObject.layer);
            }
            else if (InheritLayer)
            {
                newGo.layer = t.gameObject.layer;
            }
            if (InheritTag)
            {
                newGo.tag = t.tag;
            }
            if (InheritStaticness)
            {
                newGo.isStatic = t.gameObject.isStatic;
            }
        }

        if (!s_keep)
        {
            foreach (GameObject g in Selection.gameObjects)
            {
                GameObject.DestroyImmediate(g);
            }
        }
    }
}
