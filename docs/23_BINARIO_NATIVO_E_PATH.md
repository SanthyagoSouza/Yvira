# 23 - Binário Nativo Global do Yvira no PATH do Sistema

Agora o Yvira roda como qualquer comando nativo do sistema operacional (como `node`, `python`, `cargo` ou `rustc`), **sem necessidade de invocar o `dotnet`** ou ter o SDK instalado na máquina do usuário.

---

## 1. O Que Foi Feito

1. **Compilação Self-Contained em Arquivo Único (Single-File Binary):**
   - Publicado o runtime do Yvira com JIT embutido para Windows x64:
     ```powershell
     dotnet publish Yvira.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist\bin
     ```
   - O binário gerado foi colocado em:
     `C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\dist\bin\yvira.exe`

2. **Registro no PATH do Usuário:**
   - O caminho `...\Beta OS\dist\bin` foi registrado na variável de ambiente de usuário `Path`.
   - Agora, qualquer terminal (PowerShell, CMD, Terminal do VS Code) reconhece `yvira` globalmente.

---

## 2. Como Usar Diretamente no Terminal

Abra um novo terminal e teste os comandos:

### Informações do Sistema:
```powershell
yvira info
```

### Rodar Scripts Yvira:
```powershell
yvira run exemplos\regressao_linear\main.yv
```

### Rodar na Stack VM em Bytecode:
```powershell
yvira run --vm teste_benchmark.yv
```

### Modo Interativo com Cores (REPL):
```powershell
yvira repl
```

### Inicializar um Projeto:
```powershell
yvira init meu_projeto
cd meu_projeto
yvira run
```

---

## 3. Próximos Passos Sugeridos

Com o ambiente de desenvolvimento global configurado, temos excelentes opções para elevar o Yvira:

1. **Framework de Testes Integrado (`yvira test`):**
   - Criação de asserções nativas (`assert`, `assert_eq`).
   - Runner automático que busca arquivos `teste_*.yv` ou pastas `test/` e gera relatórios com tempo e status (pass/fail).

2. **Otimizações no Compilador de Bytecode (Peephole & Constant Folding):**
   - Dobragem de constantes em tempo de compilação (ex: `2 + 3` vira direto `5`).
   - Eliminação de opcodes redundantes e instruções mortas para ganho ainda maior de performance.

3. **Compilador de Aplicações Autônomas (`yvira build`):**
   - Transformar um script `.yv` em um executável `.exe` independente que roda sem exibir o CLI do Yvira.
