# 12 - Tipos Essenciais e Laços de Repetição

A linguagem **Yvira** foi expandida com estruturas de dados de primeira classe e estruturas modernas de repetição.

---

## 1. Strings e Manipulação de Texto

As strings em Yvira agora possuem métodos e propriedades embutidas:

```yvira
let frase = "  Linguagem Yvira e Incrível!  "

print(frase.length)          # Tamanho da string
print(frase.trim())          # Remove espaços das pontas
print(frase.upper())         # Caixa alta
print(frase.lower())         # Caixa baixa
print(frase.contains("Yvira")) # true
print(frase.sub(0, 9))       # Substring
print(frase.split(" "))      # Retorna uma Lista com as palavras divididas
```

---

## 2. Listas Heterogêneas e Dinâmicas

Diferente das matrizes puramente numéricas, as listas suportam quaisquer tipos de dados:

```yvira
let lista = ["Alice", 42, true, [1, 2]]

lista.add("Daniel")           # Adiciona item no final
let removido = lista.pop()    # Remove e retorna o último (ou índice específico)
print(lista[0])               # Acesso por índice (primeiro item)
print(lista[-1])              # Índices negativos contam do fim para o início
print(lista.join(" -> "))     # Concatena elementos
print(lista.contains(42))     # Verifica existência
```

---

## 3. Dicionários / Mapas (Chave-Valor)

Estruturas do tipo chave-valor declaradas com `{ "chave": valor }`:

```yvira
let usuario = {
    "nome": "Santhyago",
    "cargo": "Desenvolvedor",
    "nivel": 10
}

# Acesso por colchetes ou estilo membro/objeto
print(usuario["nome"])
print(usuario.cargo)

# Modificação e Métodos
usuario.set("ativo", true)
print(usuario.keys())         # Lista de chaves
print(usuario.values())       # Lista de valores
```

---

## 4. Laços de Repetição: `for` e `foreach`

### Laço `for` Tradicional (3 partes)
```yvira
let soma = 0
for (let i = 1; i <= 5; i += 1) {
    soma += i
}
print("Soma:", soma)
```

### Laço `foreach`
Itera diretamente sobre elementos de listas, caracteres de strings, ou intervalos com `range()`:

```yvira
let frutas = ["Maçã", "Banana", "Uva"]
foreach (fruta in frutas) {
    print("Fruta:", fruta)
}

# Com a função auxiliar nativa range(inicio, fim, passo)
foreach (n in range(0, 10, 2)) {
    print("Par:", n)
}
```

### Controle de Fluxo: `break` e `continue`
```yvira
for (let j = 1; j <= 10; j += 1) {
    if (j == 3) {
        continue
    }
    if (j == 7) {
        break
    }
    print(j)
}
```
