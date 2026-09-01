# Game Overview — Arcade Racer

## 1. Visão Geral e Conceito
- **Gênero:** Jogo de Corrida Arcade 3D / Time Attack / Stunt Racer
- **Estilo Visual:** Modelos 3D Low-poly, texturas pixelizadas (retro/pixel art filtering), paleta de cores vibrantes e saturadas.
- **Render Pipeline:** Universal Render Pipeline (URP 3D) para PC/Desktop.
- **Objetivo Principal:** Atravessar os níveis no menor tempo possível (Time Attack com timer visível no topo da tela) e competir por posições no ranking global (*Global Leaderboard*).

---

## 2. Referências e Inspirações
- **Jogos:**
  - *Crazy Taxi* (dinâmica arcade rápida, manobras, ritmo acelerado)
  - *Crash Bandicoot* (obstáculos dinâmicos, precisão de rota, design de fases)
  - *Mario Kart* (itens/coletáveis, pistas temáticas e rampas de impulso)
  - *Densha Attack* (alta energia, estética arcade veloz)
  - *F-Zero* (velocidade insana, boost de alto risco/recompensa)
  - *Splatoon* (cores vivas, energia visual, tipografia e UI estilizada)
  - *Trackmania* (foco em time attack puro, loops, rampas gigantes, restarts rápidos)
  - *Star Fox* (seções voadoras/aéreas, desvio de projéteis e perigos)
- **Animações / Filmes:**
  - *Redline* (sensação extrema de velocidade, deformação/linhas de velocidade, loucura arcade)
  - *Speed Racer* (2008) (pistas sinuosas neon, manobras acrobáticas, ação eletrizante)

---

## 3. Mecânicas Centrais de Gameplay

### 3.1. Timer & Leaderboard
- Timer centralizado no topo da tela com precisão de milissegundos (`00:00.000`).
- Sistema de Leaderboard global para registrar e comparar os melhores tempos por fase.

### 3.2. Sistema de Manobras (*Trick Meter*) & Mega Boost
- **Barra Roxa (Trick Gauge):** Localizada na lateral direita da tela.
- **Acumulação:** Realizar saltos, giros (flips/spins), drifts precisos e manobras no ar preenche a barra.
- **Mega Boost:** Ao atingir 100% da barra roxa:
  - Ativação de um **Mega Boost**.
  - Concede um **multiplicador de velocidade** temporário com efeitos audiovisuais intensos (linhas de velocidade, FOV warp, distorção cromática).

### 3.3. Coletáveis (*Pickups*)
1. **Relógio (Clock):** Congela/pausa o timer da corrida por 1 segundo (ótimo para otimizar time attack).
2. **Escudo (Shield):** Concede invulnerabilidade temporária contra obstáculos e colisões.
3. **Coração (Heart):** Restaura a integridade/vida (*Health*) do veículo.

### 3.4. Obstáculos e Perigos na Pista
- **Rampas acrobáticas:** Lançam o carro ao ar para realização de manobras.
- **Sessões voadoras (Gliding / Aerial Sections):** Controle aéreo temporário do veículo com propulsão.
- **Meteoros e Projéteis:** Perigos dinâmicos que caem ou atravessam a pista causando dano e perda de controle.
- **Barreiras e armadilhas:** Exigem reflexos rápidos e memorização de traçado.

### 3.5. Progressão e Customização
- Desbloqueio progressivo baseado no desempenho (medalhas/tempos/pontuação):
  - Novos chassis de veículos com estatísticas distintas (Velocidade, Aceleração, Controle, Manobra).
  - Peças customizáveis (spoilers, rodas, escapamentos).
  - Decalques, skins e paletas de cores.

