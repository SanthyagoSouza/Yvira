# 36 - Diretiva Nativa `@pdf` & Geração de Documentos e Planilhas Excel

A partir da versão 2.2, a linguagem **Yvira** traz a diretiva nativa de primeira classe **`@pdf`** e o módulo **`doc`** (Excel & Planilhas).

O grande diferencial do `@pdf` no Yvira é a **Ingestão Polimórfica Direta**: você não precisa escrever código repetitivo mapeando tupla por tupla ou montando objetos JSON intermediários. O compilador inspeciona a estrutura de dados de 3 origens e formata a paginação e a tabela automaticamente.

---

## 1. As 3 Formas de Gerar PDF no Yvira

### Fonte 1: Direto de Consultas SQL do Banco (`@db` e `@query`)
Consome diretamente o retorno de `@query(...)`. O `@pdf` detecta os nomes das colunas e os tipos de dados (números formatados com precisão e strings alinhadas):

```yvira
@db("empresa.db")

let contratos = @query("SELECT id, cliente, plano, valor, status FROM faturamento WHERE valor > 10000")

@pdf("relatorio_contratos.pdf", { "size": "A4" }) {
    title("RELATÓRIO DE FATURAMENTO CORPORATIVO", { "color": "#0f766e", "align": "center" })
    subtitle("Extraído em tempo real do banco SQLite nativo")
    text("Lista de contratos com faturamento superior a R$ 10.000,00:")

    # Ingestão direta do resultado da consulta SQL!
    table(contratos, {
        "headers": ["ID", "Cliente", "Plano Contratado", "Valor (R$)", "Situação"],
        "color": "#0d9488"
    })

    footer("Documento emitido via Yvira Data Engine")
}
```

---

### Fonte 2: A partir de Estruturas JSON ou APIs REST (`http.get`)
Se os dados vierem de arquivos `.json` ou de chamadas REST:

```yvira
let pedidosJson = [
    { "pedido": "#8091", "produto": "Servidor Yvira Enterprise", "qtd": 2, "total": 5000.0 },
    { "pedido": "#8092", "produto": "Módulo de Álgebra Linear", "qtd": 10, "total": 1500.0 }
]

@pdf("pedidos.pdf") {
    title("PEDIDOS DO E-COMMERCE", { "color": "#1e3a8a" })
    subtitle("Consumido de lista JSON / Mapas")
    
    # O table inspeciona as chaves do JSON e cria as colunas sozinho
    table(pedidosJson, { "theme": "dark" })
    
    footer("Yvira Cloud • Pedidos")
}
```

---

### Fonte 3: A partir da Própria View HTML Renderizada (`@pdf.fromView`)
Permite converter templates HTML com injeção de variáveis `{{nome}}` diretamente em PDF:

```yvira
# Exporta direto via @pdf.fromView com dados injetados
@pdf.fromView("views/certificado.html", "certificado.pdf", {
    "nome": "Santhyago Souza Ponciano",
    "curso": "Arquitetura de Compiladores e Linguagens de Programação",
    "nota": "10.0"
})
```

---

## 2. Geração de Planilhas Excel (.xlsx)

O módulo `doc` continua disponível para manipular planilhas Excel nativamente:

```yvira
import "doc" as doc

# 1. Gerando planilha com dados
let planilha = {
    "title": "Demonstrativo Financeiro Mensal",
    "headers": ["ID", "Descrição", "Valor (R$)", "Status"],
    "rows": [
        [1, "Servidor Cloud Enterprise", 2490.50, "Ativo"],
        [2, "Cluster de Banco de Dados", 3200.00, "Concluído"]
    ]
}
doc.writeExcel("demonstrativo.xlsx", planilha)

# 2. Lendo de volta
let linhas = doc.readExcel("demonstrativo.xlsx")
foreach (linha in linhas) {
    print(linha)
}
```

---

## 3. Guia de Customização da DSL `@pdf`

Dentro do bloco `@pdf(...) { ... }`, você tem acesso às seguintes funções de estilização:

| Função | Parâmetros | Descrição |
| :--- | :--- | :--- |
| `title(texto, [opcoes])` | `(string, { color?, align? })` | Define o título principal (H1) destacado no cabeçalho. |
| `subtitle(texto)` | `(string)` | Define o subtítulo secundário em cinza neutro. |
| `text(texto, [opcoes])` | `(string, { size?, color?, align?, bold?, italic? })` | Insere parágrafos de texto formatado. |
| `table(dados, [opcoes])` | `(dados, { headers?, theme?, color? })` | Desenha a tabela com ingestão polimórfica de SQL, JSON, Listas ou Matrizes. |
| `view(caminhoHtml, [dados])` | `(string, map)` | Injeta o conteúdo de um template de view dentro do fluxo do documento. |
| `footer(texto)` | `(string)` | Personaliza a mensagem do rodapé (com paginação automática *"Página X de Y"* à direita). |
