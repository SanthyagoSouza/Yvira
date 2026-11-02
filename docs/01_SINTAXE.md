# 01. Declaração de Variáveis, Sintaxe e Sistema de Tipos do Yvira

## 1. Filosofia de Design

O **Yvira** foi projetado para equilibrar alta legibilidade, agilidade no desenvolvimento e desempenho robusto.
A linguagem adota declaração explícita de variáveis via palavra-chave `let` com inferência automática de tipos em tempo de execução e na compilação da Máquina Virtual (Stack VM).

Princípios fundamentais:
- **Clareza e Simplicidade:** Sintaxe limpa, inspirada em Rust e Python.
- **Inferência Automática:** Não há necessidade de anotações verbosas de tipos.
- **Sem Declarações Fantasmas:** Toda variável de escopo deve ser iniciada com `let`.
- **Comentários Flexíveis:** Suporte a comentários de linha única com `#` e de bloco com `#- ... -#`.

---

## 2. Declaração de Variáveis com `let`

Toda criação de variável no Yvira utiliza a palavra-chave `let`:

```yvira
# Tipos Primitivos Básicos
let nome = "Santhyago";        # String
let idade = 28;                # Int
let saldo = 1550.75;           # Float / Double
let ativo = true;              # Booleano (true ou false)
let indefinido = null;         # Nulo

# Coleções Nativas
let itens = ["Notebook", "Mouse", "Teclado"]; # Lista (Array dinâmico)
let usuario = {                               # Mapa / Dicionário (Chave-Valor)
    "id": 1,
    "nome": "Santhyago",
    "cargo": "Arquiteto de Software"
};

# Álgebra Linear / Tensores Nativos
let matrizA = [[1, 2], [3, 4]];               # Matriz nativa
```

---

## 3. Tipos Suportados e Tipos NÃO Suportados

### O Que o Yvira Possui:
| Tipo | Exemplo | Descrição |
| :--- | :--- | :--- |
| `string` | `"Click Battle"` | Cadeia de caracteres Unicode |
| `int` | `100`, `-42` | Inteiros de 64 bits |
| `float` / `double` | `3.1415`, `0.05` | Ponto flutuante de precisão dupla |
| `boolean` | `true`, `false` | Valores booleanos |
| `null` | `null` | Ausência de valor |
| `list` | `[1, 2, 3]` | Vetor dinâmico com métodos (`push`, `pop`, `len`) |
| `map` | `{"a": 1, "b": 2}` | Tabela hash / Dicionário de chave-valor |
| `matrix` | `[[1, 0], [0, 1]]` | Matriz bidimensional com operadores `@`, `inv()`, `det()` |

### O Que NÃO Existe no Yvira (Evite Usar):
> [!WARNING]
> - **NÃO existe anotação de tipo explícita na declaração**: Sintaxes como `let x: int = 10` ou `x : String` **não** são suportadas pelo compilador. Use sempre `let x = 10;`.
> - **NÃO existem ponteiros brutos ou alocação manual**: O gerenciamento de memória é 100% automático e seguro.
> - **NÃO existem tipos `var`, `val` ou `const`**: Todas as declarações usam `let`.

---

## 4. Reatribuição e Modificação de Variáveis

Após declarada com `let`, a variável pode ser reatribuída livremente pelo seu identificador:

```yvira
let contador = 0;
contador = contador + 1;
print("Contador: " + contador); # Saída: Contador: 1
```

---

## 5. Operações com Coleções Nativas

### Listas:
```yvira
let lista = ["Alfa", "Beta"];
lista.push("Gama");
print("Tamanho: " + len(lista));       # 3
print("Primeiro item: " + lista[0]);  # Alfa
```

### Mapas (Dicionários):
```yvira
let config = {
    "porta": 7070,
    "ambiente": "producao"
};

config["status"] = "online";
print("Porta: " + config.porta);
print("Ambiente: " + config["ambiente"]);
```

---

## 6. Convenções de Código

- Nomes de variáveis e funções em `camelCase` ou `snake_case`.
- Ponto e vírgula `;` é opcional no final das instruções, mas recomendado para delimitador explícito.
- Nomes de classes em `PascalCase`.
