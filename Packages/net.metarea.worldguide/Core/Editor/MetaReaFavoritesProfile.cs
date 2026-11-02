using UnityEngine;

// Editor-only source of truth. Runtime receives string arrays, never this asset.
public sealed class MetaReaFavoritesProfile : ScriptableObject
{
    public string[] favoriteWorldIds = new string[0];
    public string[] favoriteWorldTitles = new string[0];
    public string[] favoriteWorldAuthors = new string[0];
}
