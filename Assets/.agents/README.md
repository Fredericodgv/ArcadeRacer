# Agent Context & Knowledge Base — Arcade Racer

Este diretório (`Assets/.agents/`) é a base de conhecimento viva e persistente para os agentes de IA que desenvolvem o projeto **Arcade Racer**.

## Arquivos de Contexto
- [`game_overview.md`](./game_overview.md): Visão geral do jogo, referências visuais/gameplay, sistema de timer, coletáveis, barra de truques (Trick Meter) e progressão.
- [`architecture.md`](./architecture.md): Diretrizes de arquitetura de cenas (MainMenu, Gameplay, UI_Overlay aditivo), ciclo de vida de managers persistentes (`DontDestroyOnLoad`), persistência e organização do código C#.

## Regras Fundamentais para Agentes
1. **Padrão de Managers:** Todo manager persistente deve seguir a guarda singleton estrita com `DontDestroyOnLoad` e destruição de instâncias duplicadas no `Awake`.
2. **Separação de UI (MVC / MVP):** Telas de UI (ex: Settings, HUD) devem atuar estritamente como *Views*, lendo e escrevendo através dos respectivos *Managers* ou *Controllers*.
3. **Pipeline URP:** Manter shaders, materiais e efeitos de pós-processamento compatíveis com o Universal Render Pipeline (URP).
4. **Carregamento Aditivo de Cenas:** Carregar `UI_Overlay` aditivamente sobre o `Gameplay`.

