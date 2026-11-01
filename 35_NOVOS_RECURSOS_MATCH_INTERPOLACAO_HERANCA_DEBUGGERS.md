# 35. Novos Recursos de Base: Match, Interpolação, Herança, Constantes e Debuggers (@die/@sleep)

Este documento registra as novas capacidades introduzidas na versão **2.1.0** da linguagem Yvira, elevando sua maturidade sintática para nível de produção.

---

## 1. Interpolação de Strings (`$"..."`)

A interpolação nativa permite injetar variáveis e expressões arbitrárias dentro de literais de texto sem a necessidade de concatenações repetidas com `+`:

```yvira
let usuario = "Santhyago";
let nivel = 99;
let vitorias = 15;

# Interpolação com expressões dinâmicas:
let msg = $"Guerreiro: {usuario} | Nível: {nivel} | Vitórias: {vitorias} | Próximo: {nivel + 1}";
print(msg);
```

---

## 2. Pattern Matching / Switch Moderno (`match`)

Substitui cadeias longas de `if / else if / else if` por uma estrutura expressiva e performática. Suporta valores exatos e curinga (`_` ou `default`):

```yvira
let evento = "iniciar_duelo";

match evento {
    "login" => print("Processando login de usuário..."),
    "iniciar_duelo" => {
        print("Duelo iniciado!");
        prepararArena();
    },
    _ => print("Evento desconhecido")
}

# Também suporta números e parênteses opcionais:
let status = 404;
match (status) {
    200 => print("OK 200"),
    404 => print("Não Encontrado"),
    _ => print("Outro status")
}
```

---

## 3. Herança e Métodos Estáticos em Classes (`extends`, `static`)

O modelo de Orientação a Objetos do Yvira agora suporta classes base, herança completa de métodos e invocação de métodos estáticos diretamente no identificador da classe:

```yvira
class Entidade {
    fn constructor(nome) {
        this.nome = nome;
    }

    fn apresentar() {
        return $"Entidade: {this.nome}";
    }

    # Método estático utilitário:
    static fn versao() {
        return "Yvira Class Engine v2.1";
    }
}

# Classe filha que herda construtor e métodos da superclasse:
class Guerreiro extends Entidade {
    fn atacar(dano) {
        return $"{this.nome} desferiu um ataque de {dano} de dano!";
    }
}

# Invocando método estático:
print(Entidade.versao());

# Instanciando classe filha:
let heroi = Guerreiro("Thor");
print(heroi.apresentar()); # Saída: Entidade: Thor
print(heroi.atacar(250));   # Saída: Thor desferiu um ataque de 250 de dano!
```

---

## 4. Constantes Rigorosas (`const`)

Variáveis declaradas com `const` são registradas no escopo com trava em tempo de execução. Qualquer tentativa de reatribuição gera exceção imediata:

```yvira
const TAXA_ARENA = 0.05;

# Tentativa de alteração:
TAXA_ARENA = 0.10;
# Lança exceção: "Erro de Execução: Não é permitido reatribuir valor à constante 'TAXA_ARENA'."
```

---

## 5. Ferramentas Reativas de Depuração: `@die` e `@sleep`

Inspiradas nas práticas mais produtivas de linguagens como o PHP, o Yvira inclui primitivas de depuração e pausa direta no fluxo do código:

### 5.1. Diretiva `@die(...)`
Imprime imediatamente um dump formatado dos valores fornecidos, dados do ambiente, timestamp e **interrompe a execução do processo na hora**.

```yvira
let usuario = { "id": 101, "nome": "Santhyago", "saldo": 3450.80 };
let itens = ["Espada", "Escudo"];

# Dump imediato e parada de execução:
@die("Inspecionando dados antes do checkout", usuario, itens);

print("Esta linha NUNCA será executada!");
```

**Saída estilizada no terminal:**
```
======================== [@die INTERRUPÇÃO DE DEBUG] ========================
[0]: Inspecionando dados antes do checkout
[1]: {"id": 101, "nome": "Santhyago", "saldo": 3450.8}
[2]: ["Espada", "Escudo"]
-----------------------------------------------------------------------------
Diretório Atual: C:\...\Beta OS
Timestamp      : 2026-10-31 17:54:01.661
=============================================================================
```

### 5.2. Diretiva `@sleep(segundos)`
Pausa o fluxo de execução pelo tempo especificado em segundos (aceita frações como `0.5` para meio segundo) sem necessidade de importar módulos:

```yvira
print("Carregando recursos...");
@sleep(1.5); # Pausa por 1 segundo e meio
print("Pronto!");
```
