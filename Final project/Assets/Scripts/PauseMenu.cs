using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    // Ссылка на нашу панель паузы из UI
    public GameObject pauseMenuPanel;

    // Переменная, которая следит, на паузе игра или нет
    private bool isPaused = false;

    void Update()
    {
        // Если игрок жмет Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // 1. Функция возврата в игру
    public void Resume()
    {
        pauseMenuPanel.SetActive(false); // Прячем панель
        Time.timeScale = 1f;            // Включаем нормальное время в игре
        isPaused = false;
    }

    // 2. Функция остановки игры
    void Pause()
    {
        pauseMenuPanel.SetActive(true);  // Показываем панель паузы
        Time.timeScale = 0f;             // Замораживаем время в игре (все остановятся)
        isPaused = true;
    }

    // 3. Функция перезапуска уровня
    public void RestartGame()
    {
        Time.timeScale = 1f; // ОБЯЗАТЕЛЬНО возвращаем время в норму перед перезапуском!
        // Перезагружаем текущую активную сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 4. Функция выхода в главное меню
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Возвращаем время в норму
        SceneManager.LoadScene(0); // Загружаем сцену меню (она у нас под индексом 0)
    }
}