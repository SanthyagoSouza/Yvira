# Exemplo Prático de Machine Learning em Yvira: Regressão Linear com Mínimos Quadrados (OLS)

Este projeto demonstra a capacidade computacional da linguagem **Yvira** unindo:
1. **Orientação a Objetos (`class RegressaoLinear`)**: Encapsulamento dos pesos e métodos de treino e previsão.
2. **Motor Científico Matricial Nativo**:
   - Transposição de matrizes ($X^T$).
   - Multiplicação matricial ($X^T X$).
   - Inversão numérica com pivotamento parcial ($(X^T X)^{-1}$).
   - Cálculo da Equação Normal analítica: $\beta = (X^T X)^{-1} X^T y$.
3. **Módulo de Sistema de Arquivos (`fs`)**:
   - Leitura de dataset de treino (`imoveis.csv`).
   - Exportação automática das predições para arquivo CSV (`previsoes_mercado.csv`).

---

## Estrutura do Projeto

```
exemplos/regressao_linear/
├── imoveis.csv             <- Dataset com área em m², quantidade de quartos e preço
├── main.yv                 <- Código fonte completo em Yvira
└── previsoes_mercado.csv   <- Relatório de predições gerado pelo modelo
```

---

## Como Executar

Para rodar a aplicação através do CLI da linguagem Yvira:

```bash
yvira run exemplos/regressao_linear/main.yv
```

---

## Resultados Obtidos

O modelo calcula automaticamente os parâmetros ótimos:
- **Intercepto ($\beta_0$)**: ~R$ 18,66 mil
- **Preço por $m^2$ ($\beta_1$)**: ~R$ 2,78 mil por $m^2$
- **Preço por quarto ($\beta_2$)**: ~R$ 4,80 mil por quarto

### Simulações de Novos Imóveis:
- **70 $m^2$, 2 quartos**  -> R$ 222.839,57
- **120 $m^2$, 3 quartos** -> R$ 366.621,13
- **200 $m^2$, 4 quartos** -> R$ 593.793,39