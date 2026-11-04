using UnityEngine;

// Editor-only source of truth. Runtime receives string arrays, never this asset.
public sealed class MetaReaFavoritesProfile : ScriptableObject
{
    public int folderSchemaVersion;
    public MetaReaFavoriteFolder[] folders = new MetaReaFavoriteFolder[0];
    public string[] favoriteWorldIds = new string[0];
    public string[] favoriteWorldTitles = new string[0];
    public string[] favoriteWorldAuthors = new string[0];
}

[System.Serializable]
public sealed class MetaReaFavoriteFolder
{
    public string name = "新しいフォルダ";
    public string[] favoriteWorldIds = new string[0];
    public string[] favoriteWorldTitles = new string[0];
    public string[] favoriteWorldAuthors = new string[0];
}
