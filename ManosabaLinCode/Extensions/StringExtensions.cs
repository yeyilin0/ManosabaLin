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
        
  

// ↓ 新增
        public string EmalinCardsImagePath()
        {
            return BuildResPath("images", "cards", "emalin", path);
        }

        public string EmalinBigCardsImagePath()
        {
            return BuildResPath("images", "cards", "emalin", "big", path);
        }
// ↑ 新增

        public string EmalinPowerImagePath()
        {
            return BuildResPath("images", "powers", path);
        }
    }
}
