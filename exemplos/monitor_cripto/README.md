# Monitor Financeiro de Criptoativos em Tempo Real (Yvira)

Este projeto demonstra a capacidade de **comunicação de rede e I/O de alta produtividade** da linguagem **Yvira**.

---

## Funcionalidades Demonstradas

1. **Variáveis de Ambiente (`.env`)**:
   - Importação via `import ".env" as env`.
   - Parametrização da moeda base, provedor da API e nome do relatório de saída.

2. **Consumo de API REST Externa (`http`)**:
   - Requisição `http.get(...)` síncrona com verificação de status `res.ok`.
   - Desserialização de JSON aninhado via `res.json()`.

3. **Modelagem Orientada a Objetos (`class CriptoAtivo`)**:
   - Criação de classes para estruturar os dados de cada ativo (Bitcoin, Ethereum, Solana).
   - Métodos de formatação de strings e manipulação de estado.

4. **Matrizes Nativas e Estatística**:
   - Conversão dos dados de mercado para uma matriz bidimensional 3x3:
     - Coluna 0: Preço em USD
     - Coluna 1: Preço em BRL
     - Coluna 2: Variação percentual nas últimas 24 horas
   - Cálculo automático da variação média ponderada da cesta.

5. **Persistência de Dados em Disco (`fs`)**:
   - Exportação da matriz financeira para arquivo CSV (`cotacoes_resumo.csv`) usando `fs.writeCsv()`.
   - Registro de histórico de auditoria usando `fs.append("auditoria.log", ...)`.

---

## Estrutura do Projeto

```
exemplos/monitor_cripto/
├── .env                  <- Configurações de ambiente
├── main.yv               <- Script principal em Yvira
├── cotacoes_resumo.csv   <- Matriz exportada em CSV com cotações atuais
├── auditoria.log         <- Log com histórico de requisições
└── README.md             <- Documentação do projeto
```

---

## Como Executar

Execute o comando através do compilador/CLI Yvira:

```bash
yvira run exemplos/monitor_cripto/main.yv
```