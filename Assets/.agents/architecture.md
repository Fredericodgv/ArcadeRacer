# Architecture & Technical Guidelines — Arcade Racer

## 1. Render Pipeline & Plataforma
- **Engine:** Unity (Universal 3D / URP — Universal Render Pipeline).
- **Alvo:** PC / Desktop.
- **Visual:** Shaders/materiais URP com estilo retro/low-poly (unlit ou simple lit, pixelated textures, post-processing vibrante como bloom, chromatic aberration, vignette).

---

## 2. Arquitetura de Cenas
1. **MainMenu (`Assets/Scenes/MainMenu.unity`):**
   - Cena inicial do jogo.
   - Ponto de entrada onde os **Managers Persistentes** são instanciados e mantidos via `DontDestroyOnLoad`.
   - UI de Menu Principal: Jogar, Garagem/Customização, Leaderboards, Configurações, Sair.
2. **Gameplay (`Assets/Scenes/Gameplay.unity` / Níveis):**
   - Cena que contém a pista, iluminação do mundo, spawn point, física e obstáculos.
3. **UI_Overlay (`Assets/Scenes/UI_Overlay.unity`):**
   - HUD de gameplay (Timer, Trick Meter / Barra Roxa, Velocímetro, Vida, Notificações de Truques/Pickups).
   - Telas de Pause e Fim de Fase (Resultados / Leaderboard).
   - **Modo de Carregamento:** Carregado de forma aditiva (`LoadSceneMode.Additive`) sobre a cena de Gameplay em vez de trocar a cena inteira.

---

## 3. Padrão Obrigatório para Managers Persistentes
Todos os Managers persistentes instanciados no `MainMenu` devem implementar o padrão Singleton com guarda contra duplicação no método `Awake`:

```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

### Managers Principais:
- **`SettingsManager`:**
  - Carrega, salva e aplica configurações globais (Áudio: Master/Music/SFX; Gráficos: Resolução, Fullscreen, Qualidade, VSync; Gameplay: Sensibilidade, Inversão de Eixo, FOV).
  - Estrutura de dados: `SettingsData` (serializável / JSON em `Application.persistentDataPath` ou `PlayerPrefs`).
  - As telas de Settings (MainMenu ou Pause no Gameplay) são puramente *Views* que consomem e atualizam o `SettingsManager`.
- **`AudioManager`:**
  - Gerenciamento de trilhas sonoras dinâmicas (BGM arcade acelerada) e efeitos sonoros (SFX de motor, turbo, drift, batida, pickups, manobras, mega boost).
  - Controle de barramentos de áudio via AudioMixer.
- **`SaveSystem` / `ProgressionManager`:**
  - Persistência de recordes locais, medalhas, veículos e peças desbloqueadas.
- **`GameManager` / `SceneLoader`:**
  - Controle de fluxo de estados do jogo (MainMenu, Loading, Playing, Paused, GameOver, Victory) e carregamento assíncrono/aditivo de cenas.

---

## 4. Convenções de Código e Estrutura de Pastas
- `Assets/Scripts/Core/` — Managers persistentes (GameManager, SettingsManager, AudioManager, SaveSystem).
- `Assets/Scripts/Vehicle/` — CarController, Stunt/TrickDetector, CarPhysics, VisualEffects.
- `Assets/Scripts/Track/` — Obstacles (meteors, ramps, moving hazards), Checkpoints, FinishLine, GlidingZones.
- `Assets/Scripts/Pickups/` — Base Pickup, ClockPickup, ShieldPickup, HeartPickup.
- `Assets/Scripts/UI/` — HUD, TrickMeterView, TimerView, MainMenuView, SettingsView, PauseView, LeaderboardView.
- `Assets/Scripts/Data/` — ScriptableObjects (CarData, TrackData, SettingsData).

---

## 5. Diretriz de Desenvolvimento com o Usuário
- **Configuração no Editor:** Sempre priorizar expor campos (`[SerializeField]`) e permitir que o usuário configure componentes, físicas, colisores, materiais e layers diretamente pelo Inspector da Unity.
- **Evitar Hardcode:** Não sobrescrever via código configurações padrão da Unity (ex: `rb.mass`, `rb.drag`, etc.) a menos que estritamente necessário.
- **Documentação Obrigatória com `/// <summary>`:** Todos os métodos de física, cálculos vetoriais e regras de gameplay devem conter docstrings XML `/// <summary>` claras e detalhadas explicando a física/lógica aplicada, mantendo-os rigorosamente atualizados a cada refatoração.
- **Comunicação:** O usuário já possui conhecimento de Unity. Fornecer instruções breves, diretas e objetivas sobre quais configurações ou componentes ajustar no Editor.



