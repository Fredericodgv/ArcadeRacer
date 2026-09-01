# 🏎️ Arcade Racer

> Um jogo de corrida arcade 3D de alta velocidade, visual low-poly com texturas pixelizadas e cores vibrantes, focado em manobras acrobáticas, pistas insanas e recordes mundiais.

---

## 🎮 Sobre o Jogo

**Arcade Racer** combina a velocidade vertiginosa e o risco/recompensa de clássicos das corridas arcade com a precisão de um jogo de *Time Attack*. O objetivo principal é simples: cruzar a linha de chegada o mais rápido possível enquanto desvia de perigos cósmicos, executa manobras radicais e coleta itens pelo trajeto para registrar seu nome no topo da **Leaderboard Global**.

### ✨ Principais Características

- ⏱️ **Time Attack & Leaderboard Global:** Timer com precisão de milissegundos no topo da tela e ranking mundial competitivo.
- ⚡ **Barra de Truques (*Trick Meter*) & Mega Boost:** Uma barra roxa na lateral direita da tela que se enche conforme você acerta manobras, giros e saltos acrobáticos. Ao atingir 100%, acione um **Mega Boost** com multiplicador de velocidade insano e efeitos visuais impressionantes.
- 🚀 **Pistas Radicais & Seções Aéreas:** Pistas repletas de rampas gigantescas, seções voadoras (*gliding*), meteoros em queda, loops e armadilhas dinâmicas.
- 🎁 **Coletáveis Estratégicos:**
  - ⏱️ **Relógio:** Pausa o cronômetro da corrida por 1 segundo.
  - 🛡️ **Escudo:** Concede invulnerabilidade temporária contra colisões e perigos.
  - ❤️ **Coração:** Regenera a integridade/vida do veículo.
- 🎨 **Customização & Garagem:** Desbloqueie novos veículos com diferentes atributos (Velocidade, Aceleração, Manobrabilidade), além de peças, escapamentos, spoilers e decalques exclusivos com base no seu desempenho.

---

## 🕹️ Referências & Inspirações

| Categoria | Referências |
| :--- | :--- |
| **Jogos** | *Crazy Taxi*, *Crash Bandicoot*, *Mario Kart*, *Densha Attack*, *F-Zero*, *Splatoon*, *Trackmania*, *Star Fox* |
| **Animações / Filmes** | *Redline* (estética extrema de velocidade e adrenalina), *Speed Racer* (pistas sinuosas e manobras neon) |

---

## 🛠️ Arquitetura Técnica

- **Engine:** Unity (Universal Render Pipeline — URP 3D)
- **Plataforma:** PC / Desktop
- **Input:** Unity New Input System (`InputSystem_Actions`)

### Fluxo de Cenas
1. **`MainMenu`:** Cena inicial onde os managers persistentes são carregados via `DontDestroyOnLoad`.
2. **`Gameplay`:** Contém a pista, física do carro, spawn point, obstáculos e iluminação da fase.
3. **`UI_Overlay`:** Carregada de forma **aditiva** (`LoadSceneMode.Additive`) sobre a cena de Gameplay para gerenciar HUD, timer, medidor de manobras e menus de pausa sem recarregar o cenário.

### Padrão de Managers Persistentes
Os managers centrais (`SettingsManager`, `AudioManager`, `SaveSystem`) seguem o padrão Singleton com destruição automática de duplicatas no `Awake`:

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

---

## 📂 Estrutura do Projeto

```text
ArcadeRacer/
├── Assets/
│   ├── .agents/                 # Documentação técnica e contexto para agentes de IA
│   │   ├── architecture.md      # Padrões de código e fluxo de cenas
│   │   ├── game_overview.md     # Detalhamento de mecânicas e game design
│   │   └── README.md            # Índice da base de conhecimento
│   ├── InputSystem_Actions/     # Mapeamento do New Input System
│   ├── Scenes/                  # Cenas do jogo (MainMenu, Gameplay, UI_Overlay)
│   ├── Scripts/                 # Código-fonte C#
│   │   ├── Core/                # Managers (SettingsManager, AudioManager, GameManager)
│   │   ├── Vehicle/             # Controlador de física, manobras e boost
│   │   ├── Track/               # Obstáculos, rampas, meteoros e checkpoints
│   │   ├── Pickups/             # Coletáveis (Relógio, Escudo, Coração)
│   │   ├── UI/                  # HUD, Views de Menu, Pause e Leaderboard
│   │   └── Data/                # ScriptableObjects e modelos de dados
│   └── Settings/                # Configurações URP e de Renderização
└── README.md                    # Documentação principal do projeto
```

---

## 🚀 Como Executar o Projeto

1. **Pré-requisitos:**
   - Unity Editor compatível com URP (versão 6000.x ou Unity 2022.3+ LTS).
   - Suporte a Desktop (Windows / macOS / Linux).

2. **Abrindo o Projeto:**
   - Clone ou abra a pasta do projeto no **Unity Hub**.
   - Abra a cena inicial em `Assets/Scenes/MainMenu.unity`.
   - Pressione **Play** no editor.

---

## 🎮 Esquema de Controles

### 🕹️ Mapeamento do Gamepad

```text
                     ┌──────────────────┐               ┌──────────────────┐
                     │    [ L2 / LT ]   │               │    [ R2 / RT ]   │
                     │    Frear / Ré    │               │     Acelerar     │
                     └────────┬─────────┘               └────────┬─────────┘
                              │                                  │
    ┌──────────────────┐      │                                  │      ┌──────────────────┐
    │    [ L1 / LB ]   │◄─────┤                                  ├─────►│    [ R1 / RB ]   │
    │    Mega Boost    │      │                                  │      │ Drift / Derrapagem
    └──────────────────┘      │     .----------------------.     │      └──────────────────┘
                              └───►/   [View]      [Menu]   \◄───┘
                                  /       \          /       \
     ┌──────────────────┐        |  (▲)    `--------'    (Y)  |        ┌──────────────────┐
     │ Analógico Esq.   ├───────►| (◄)(►)                (X)(B)|◄───────┤ Botões de Ação   │
     │ Direção / Curva  │        |  (▼)    (L)    (R)    (A)  |        │ (Y) Mega Boost   │
     └──────────────────┘         \         \      /         /         │ (X) Giro Aéreo   │
                                   \         `----'         /          │ (B) Freio de Mão │
                                    `----------------------'           │ (A) Drift / Pulo │
                                              ▲                        └──────────────────┘
                                              │
                                    ┌─────────┴────────┐
                                    │  Analógico Dir.  │
                                    │ Manobras Aéreas  │
                                    │ (Flips & Rolls)  │
                                    └──────────────────┘
```

### ⌨️ Tabela Completa de Controles

| Ação | Gamepad (Xbox / PlayStation) | Teclado & Mouse |
| :--- | :--- | :--- |
| **Acelerar** | Gatilho Direito (`RT` / `R2`) | `W` |
| **Frear / Marcha Ré** | Gatilho Esquerdo (`LT` / `L2`) | `S`  |
| **Direção** | Analógico Esquerdo (`LS`) / D-Pad | `A` / `D` |
| **Drift / Derrapagem** | Botão de Ombro (`RB` / `R1`) ou Botão `A` / `✕` | `Espaço` / `Shift Esquerdo` |
| **Manobras Aéreas (Flips / Spins)** | Botão `A` / `x` + Analógico Esquerdo (`LS`) | `Espaço` + WASD |
| **Ativar Mega Boost (Barra Roxa)** | Botão de Ombro (`LB` / `L1`) ou Botão `Y` / `△` | `F` / `Shift Direito` |
| **Freio de Mão Rápido** | Botão `B` / `◯` | `Ctrl Esquerdo` |
| **Pausar / Menu** | Botão `Menu` (`Start` / `Options`) | `Esc` |

---

## 📄 Licença

Projeto desenvolvido para fins de entretenimento e estudo. Todos os direitos reservados aos autores.

