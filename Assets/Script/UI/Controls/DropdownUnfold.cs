/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script onto any TextMeshPro Dropdown. Tools > UI > Build Main Title Scene puts it on the New Game
        dropdowns (Win at, Start time).
Unity Editor: Nothing to connect. Testing: Play and open the dropdown.
Purpose: Opening the dropdown unfolds its list downward (it grows from flat to full height, with a little bounce)
         instead of just appearing.
Requirement: DropdownUnfold.cs x-> UITween.cs, TMP_Dropdown (Unity)
             DropdownUnfold.cs <-x TitleSceneBuilder.cs
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
[DisallowMultipleComponent]
public class DropdownUnfold : MonoBehaviour
{
    // ===== Fields =====
    const string ListName = "Dropdown List";   // the name TextMeshPro gives the open list

    Transform openList;   // the list being shown now (null = closed)

    // ===== Unity messages (Unity calls these) =====

    // The dropdown makes its list when it opens and removes it when it closes, so watch for a new one.
    void LateUpdate()
    {
        Transform list = transform.Find(ListName);
        if (list == openList) return;

        openList = list;
        if (list == null) return;

        list.localScale = new Vector3(1f, 0f, 1f);
        UITween.Scale(this, list, Vector3.one, 0.28f, UITween.Ease.OutBack);
    }
}
