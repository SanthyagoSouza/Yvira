# 15 - Orientação a Objetos (Classes, Construtor e Métodos)

A linguagem **Yvira** agora suporta Orientação a Objetos de forma simples, expressiva e moderna.

---

## 1. Declaração de Classe e Construtor (`init`)

As classes são declaradas com a palavra-chave `class`. Para definir o construtor da classe, basta declarar o método `fn init(...)`:

```yvira
class Pessoa {
    fn init(nome, idade) {
        this.nome = nome
        this.idade = idade
    }

    fn apresentar() {
        print("Olá! Meu nome é " + this.nome + " e tenho " + this.idade + " anos.")
    }

    fn fazerAniversario() {
        this.idade += 1
    }
}
```

---

## 2. Instanciação e Acesso a Atributos

Para criar um novo objeto da classe, basta invocar a classe como uma função:

```yvira
let p1 = Pessoa("Carlos", 25)

p1.apresentar()        # Executa método da instância
p1.fazerAniversario()  # Modifica this.idade diretamente

p1.nome = "Carlos Eduardo" # Atribuição direta a atributos
print(p1.nome)             # Carlos Eduardo
print(p1.idade)            # 26
```

---

## 3. Estado Encapsulado e Métodos de Negócio

Você pode combinar métodos com lógica condicional, laços e exceções normalmente:

```yvira
class ContaBancaria {
    fn init(titular, saldoInicial) {
        this.titular = titular
        this.saldo = saldoInicial
    }

    fn depositar(valor) {
        if (valor <= 0) {
            throw "O valor do depósito deve ser positivo."
        }
        this.saldo += valor
    }

    fn sacar(valor) {
        if (valor > this.saldo) {
            throw "Saldo insuficiente."
        }
        this.saldo -= valor
        return this.saldo
    }
}

let conta = ContaBancaria("Santhyago", 1000)
conta.depositar(500)
conta.sacar(200)
print(conta) # <ContaBancaria { titular: Santhyago, saldo: 1300 }>
```

---

## 4. Destaques do Design
- **Sem complexidade desnecessária**: Sem boilerplate de modificadores como `public`, `private` ou geradores de getter/setter.
- **`this` explícito**: Os métodos usam `this.campo` para ler e alterar o estado do objeto.
- **Representação legível**: Ao dar `print(objeto)`, a instância é formatada com seu nome de classe e seus campos.