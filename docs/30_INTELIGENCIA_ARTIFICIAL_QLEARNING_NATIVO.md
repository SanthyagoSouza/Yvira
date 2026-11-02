# 30. Inteligência Artificial: Aprendizado por Reforço Nativo (Q-Learning)

O Yvira suporta o desenvolvimento de algoritmos de **Inteligência Artificial e Machine Learning** de forma 100% nativa, aproveitando seus tipos de primeira classe de **Álgebra Linear e Matrizes** (`zeros`, `ones`, `eye`, operações matriciais e indexação bidimensional `mat[r, c]`).

Este exemplo demonstra um agente autônomo baseado no algoritmo clássico de **Q-Learning (Equação de Bellman)** resolvendo um labirinto dinâmico com obstáculos e armadilhas.

---

## 1. Fundamentos Matemáticos: A Equação de Bellman

O agente mantém uma **Q-Table** modelada diretamente como uma matriz no Yvira:
* **Linhas:** Estados possíveis do ambiente (cada célula da grade $5 \times 5 = 25$ estados).
* **Colunas:** Ações disponíveis ($0: \text{Cima}, 1: \text{Baixo}, 2: \text{Esquerda}, 3: \text{Direita}$).

A cada passo, o agente atualiza o valor da utilidade esperada da ação através da fórmula:

$$Q(s, a) \leftarrow Q(s, a) + \alpha \cdot \left[ R + \gamma \cdot \max_{a'} Q(s', a') - Q(s, a) \right]$$

Onde:
* $\alpha$ (Alpha): Taxa de aprendizado (`learning rate`).
* $\gamma$ (Gamma): Fator de desconto para recompensas futuras (`discount factor`).
* $\epsilon$ (Epsilon): Política $\epsilon$-greedy para equilibrar exploração aleatória vs conhecimento adquirido.

---

## 2. Estrutura do Projeto

O projeto está organizado em módulos orientados a objetos na pasta `exemplos/ia_qlearning_labirinto/`:

* **`ambiente.yv`**: Classe `LabirintoAmbiente` contendo a grade de estados, paredes (`🧱`), armadilhas de lava (`🔥`) e tesouro (`💎`), cálculo de recompensas e renderizador visual ASCII.
* **`agente.yv`**: Classe `QLearningAgent` contendo a `qTable = zeros(totalEstados, totalAcoes)`, política $\epsilon$-greedy e função `aprender(...)`.
* **`main.yv`**: Orquestrador do treinamento de 1.000 episódios e demonstração do trajeto passo a passo.

---

## 3. Como Executar

Abra o terminal e execute:

```powershell
cd "exemplos\ia_qlearning_labirinto"
yvira run main.yv
```

### Resultados Obtidos:
* **Velocidade de Treinamento:** 1.000 episódios simulados em **apenas 760 milissegundos**!
* **Taxa de Sucesso:**
  * Primeiros 500 episódios: **~70%** (fase de exploração e quedas na lava)
  * Últimos 500 episódios: **98.6%** (alta precisão e convergência)
* **Demonstração:** O agente encontra o caminho ótimo de **8 passos perfeitos** até o tesouro, desviando de todas as paredes e armadilhas.
