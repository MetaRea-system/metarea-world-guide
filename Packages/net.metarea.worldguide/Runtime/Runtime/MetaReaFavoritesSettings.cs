using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MetaReaFavoritesSettings : UdonSharpBehaviour
{
    [HideInInspector] public string[] favoriteWorldIds = new string[0];
    [HideInInspector] public string[] favoriteWorldTitles = new string[0];
    [HideInInspector] public string[] favoriteWorldAuthors = new string[0];
}
