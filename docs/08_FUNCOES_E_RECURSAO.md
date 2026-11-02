# 08. Funções, Recursão e Escopo no Yvira

## 1. Declaração de Funções

No Yvira, todas as funções são declaradas utilizando a palavra-chave `fn` seguida do nome da função, lista de parâmetros e o corpo entre chaves `{ ... }`:

```yvira
fn somar(a, b) {
    return a + b;
}

let resultado = somar(10, 20);
print("Resultado: " + resultado); # Saída: 30
```

---

## 2. Parâmetros e Retorno de Valores

- **Múltiplos parâmetros:** São separados por vírgula.
- **Retorno (`return`):** A instrução `return` encerra a execução da função imediatamente e devolve o valor especificado. Se nenhum valor for informado (`return;`), a função retorna `null`.

```yvira
fn verificarMaioridade(nome, idade) {
    if (idade >= 18) {
        return nome + " é maior de idade (" + idade + " anos)";
    }
    return nome + " é menor de idade (" + idade + " anos)";
}

print(verificarMaioridade("Lucas", 21));
```

---

## 3. O Que o Yvira Suporta e NÃO Suporta em Funções

### O Que é Suportado:
- **Funções como Cidadãs de Primeira Classe:** Você pode passar funções como argumento para outras funções ou callbacks de eventos:
  ```yvira
  fn aoReceber(dados) {
      print("Recebido: " + dados);
  }
  rede.ao("notificacao", aoReceber);
  ```
- **Recursão:** Suporte completo a funções recursivas e call frames isolados:
  ```yvira
  fn fatorial(n) {
      if (n <= 1) { return 1; }
      return n * fatorial(n - 1);
  }
  print("Fatorial de 5: " + fatorial(5)); # 120
  ```

### O Que NÃO é Suportado:
> [!WARNING]
> - **NÃO existem funções anônimas inline (`lambda` ou `arrow functions`)**:
>   - ❌ **Incorreto:** `@time.executar(fn() { ... });` ou `lista.map(x => x * 2);`
>   - ✅ **Correto:** Declare a função com nome e passe seu identificador:
>     ```yvira
>     fn rotina() {
>         print("Executando rotina...");
>     }
>     @time.rotina(2);
>     ```
> - **NÃO há sobrecarga de métodos pelo mesmo nome**: Cada função no mesmo escopo deve ter um nome único.
