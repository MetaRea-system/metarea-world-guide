using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MetaReaFavoritesSettings : UdonSharpBehaviour
{
    [HideInInspector] public bool favoriteFlatList;
    [HideInInspector] public string[] favoriteFolderDescriptions = new string[0];
    [HideInInspector] public string[] favoriteFolderNames = new string[0];
    [HideInInspector] public int[] favoriteFolderIndices = new int[0];
    [HideInInspector] public string favoritesProfileGuid = "";
    [HideInInspector] public string[] favoriteWorldIds = new string[0];
    [HideInInspector] public string[] favoriteWorldTitles = new string[0];
    [HideInInspector] public string[] favoriteWorldAuthors = new string[0];
}
