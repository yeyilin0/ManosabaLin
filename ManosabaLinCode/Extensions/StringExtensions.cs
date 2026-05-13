using static ManosabaLin.Utils.ResourcePathHelper;

namespace ManosabaLin.Extensions;

public static class StringExtensions
{
    extension(string path)
    {
        public string ImagePath()
        {
            return BuildResPath("images", path);
        }

        public string CharacterImgPath(string character)
        {
            return BuildResPath("images", "characters", character, path);
        }

        public string CharacterAudioPath()
        {
            return BuildRelativePath("characters", path);
        }

        public string CharacterScenePath(string character)
        {
            return BuildResPath("scenes", character, path);
        }

        public string PotionImagePath()
        {
            return BuildResPath("images", "potions", path);
        }

        public string CardsImagePath()
        {
            return BuildResPath("images", "cards", path);
        }

        public string CardsAudioPath()
        {
            return BuildRelativePath("cards", path);
        }

        public string BgmAudioPath()
        {
            return BuildRelativePath("bgm", path);
        }

        public string BigCardsImagePath()
        {
            return BuildResPath("images", "cards", "big", path);
        }

        public string PowerImagePath()
        {
            return BuildResPath("images", "powers", path);
        }

        public string BigPowerImagePath()
        {
            return BuildResPath("images", "powers", "big", path);
        }

        public string RelicImagePath()
        {
            return BuildResPath("images", "relics", path);
        }

        public string BigRelicImagePath()
        {
            return BuildResPath("images", "relics", "big", path);
        }

        public string CharacterUiPath()
        {
            return BuildResPath("images", "charui", path);
        }

        public string EventBackgroundScenePath()
        {
            return BuildResPath("scenes", "events", "background_scenes", path);
        }

        public string EventBackgroundImagePath()
        {
            return BuildResPath("images", "events", "background_scenes", path);
        }

        public string AncientMapIconPath()
        {
            return BuildResPath("images", "map", "ancients", path);
        }

        public string RunHistoryIconPath()
        {
            return BuildResPath("images", "ui", "run_history", path);
        }
    }
}
