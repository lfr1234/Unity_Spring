using UnityEngine;
using UnityEngine.SceneManagement;
using SpringCityMessenger.Core;
using SpringCityMessenger.Systems;

public class SceneSwitcher : MonoBehaviour
{
    public string migrationSceneName = "Scenes/MigrationScene";
    public string homeSceneName = "Scenes/HomeScene";

    public void GoToMigrationScene()
    {
        // 出发迁徙条件（按策划案关键门槛）
        var seagull = FindObjectOfType<SeagullStatus>();
        if (seagull == null)
        {
            GameMessageUI.Show("未找到海鸥状态，无法出发。");
            return;
        }

        if (seagull.level < 3)
        {
            GameMessageUI.Show("等级不足：需要 Lv.3 才能迁徙。");
            return;
        }
        if (seagull.stamina < 50)
        {
            GameMessageUI.Show("体力不足：至少需要 50 才能出发。");
            return;
        }
        if (seagull.health < 30)
        {
            GameMessageUI.Show("健康过低：至少需要 30 才能出发。");
            return;
        }
        if (seagull.hunger < 30)
        {
            GameMessageUI.Show("饱食过低：至少需要 30 才能出发。");
            return;
        }
        if (seagull.isSick)
        {
            GameMessageUI.Show("海鸥生病中，不能出发。");
            return;
        }

        // 行囊必须有食物（策划案：行囊主食必填）
        var backpack = FindObjectOfType<BackpackSystem>();
        if (backpack == null || backpack.TotalFoodCount <= 0)
        {
            GameMessageUI.Show("行囊里没有食物，请先在背包面板点击“装进行囊”。");
            return;
        }

        // 条件都达标时，先自动存一份当前进度，避免你忘记点“保存”
        var save = FindObjectOfType<GameSaveManager>();
        if (save != null)
        {
            save.SaveGame();
        }

        SceneManager.LoadScene(migrationSceneName);
    }

    public void GoToHomeScene()
    {
        SceneManager.LoadScene(homeSceneName);
    }
}