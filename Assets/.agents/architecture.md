# Architecture & Technical Guidelines — Arcade Racer

## 1. Render Pipeline & Plataforma
- **Engine:** Unity 6 (6000.x), Universal Render Pipeline (URP 3D).
- **Alvo:** PC / Desktop.
- **Visual:** Shaders URP low-poly (Unlit / Simple Lit), texturas pixelizadas, post-processing vibrante (Bloom, Chromatic Aberration, Vignette).

---

## 2. Arquitetura de Cenas

| Cena | Função |
|---|---|
| `MainMenu.unity` | Ponto de entrada; instancia Managers Persistentes com `DontDestroyOnLoad`. |
| `Gameplay.unity` | Pista, física, spawn point, obstáculos, iluminação. |
| `UI_Overlay.unity` | HUD (Timer, Trick Meter, Velocímetro, Vida); carregada com `LoadSceneMode.Additive` sobre Gameplay. |

---

## 3. Padrão Singleton para Managers Persistentes

```csharp
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

### Managers previstos:
- **`GameManager`** — Estado do jogo (Playing, Paused, GameOver) e carregamento assíncrono de cenas.
- **`AudioManager`** — BGM e SFX via AudioMixer (motor, turbo, drift, pickups, boost).
- **`SettingsManager`** — Configurações salvas em JSON (`SettingsData`): áudio, gráficos, controles.
- **`SaveSystem`** — Recordes, medalhas, veículos e peças desbloqueadas.

---

## 4. Estrutura de Pastas

```
Assets/
  Scripts/
    Core/      — GameManager, AudioManager, SettingsManager, SaveSystem
    Vehicle/   — CarController, TrickDetector, VehicleVFX
    Track/     — Checkpoints, FinishLine, Hazards (meteors, ramps, gliding zones)
    Pickups/   — PickupBase, ClockPickup, ShieldPickup, HeartPickup
    UI/        — HUD, TrickMeterView, TimerView, SettingsView, PauseView
    Data/      — ScriptableObjects (CarData, TrackData, SettingsData)
```

---

## 5. Diretrizes de Desenvolvimento

- **Editor First:** Expor via `[SerializeField]` tudo que o usuário puder ajustar no Inspector (massa, drag, layers, curvas). Nunca hardcodar `rb.mass`, `rb.drag` etc. no código.
- **Input System:** Usar `InputSystem.actions.FindAction("Player/<Action>")` no `Awake`. Não usar componente `PlayerInput` com o asset global (conflito de singleton).
- **Documentação `/// <summary>`:** Obrigatório em todo método de física, cálculo vetorial e regra de gameplay. Manter atualizado a cada refatoração.
- **Comunicação:** Usuário tem experiência em Unity. Instruções breves e diretas sobre o Inspector; sem tutoriais básicos.

---

## 6. Regras Anti-Jitter (Física)

> Lições aprendidas na implementação do `CarController`:

- **Nunca corte força bruscamente:** Use `powerFactor = Clamp01(1 - speed/maxSpeed)` para curva suave.
- **Uma única `MoveRotation` por `FixedUpdate`:** Combinar esterço + alinhamento de terreno em um Slerp único.
- **Grip lateral com `Time.fixedDeltaTime`:** `rb.linearVelocity -= sideVel * Clamp01(rate * fixedDeltaTime)`.
- **Câmera em `LateUpdate`** com `Mathf.LerpAngle` no eixo Y.
- **Air Tricks:** Pós-multiplicação de Quaternion (`rb.rotation * deltaRot`) para evitar Gimbal Lock nos 90°.
