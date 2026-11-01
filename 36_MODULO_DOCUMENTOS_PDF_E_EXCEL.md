# 36 - Módulo Nativo de Documentos: Geração de PDF e Planilhas Excel (`doc`)

A partir da versão 2.2, a linguagem **Yvira** passa a contar com o módulo nativo `doc` (Document Engine), permitindo gerar e ler relatórios executivos em **PDF** com tabelas paginadas e planilhas **Excel (.xlsx)** estilizadas sem depender de suítes de escritório ou servidores externos.

---

## 1. Importação do Módulo

O módulo é integrado à Standard Library do runtime Yvira:

```yvira
import "doc" as doc
```

---

## 2. Geração de Planilhas Excel (.xlsx)

O comando `doc.writeExcel` permite criar pastas de trabalho completas com cabeçalhos estilizados, linhas e tipagem automática (texto, números, booleanos).

### Criando uma Planilha:
```yvira
import "doc" as doc

let planilha = {
    "title": "Demonstrativo Financeiro Mensal",
    "headers": ["ID", "Descrição", "Valor (R$)", "Status"],
    "rows": [
        [1, "Servidor Cloud Enterprise", 2490.50, "Ativo"],
        [2, "Cluster de Banco de Dados", 3200.00, "Concluído"],
        [3, "Licenças de Usuário", 850.00, "Ativo"]
    ]
}

doc.writeExcel("demonstrativo.xlsx", planilha)
print("Planilha Excel gerada com sucesso!")
```

### Exportando Matrizes Científicas Diretamente para Excel:
Você pode passar uma matriz nativa do Yvira diretamente:
```yvira
let M = [[10.5, 20.3], [30.1, 40.8]]
doc.writeExcel("matriz.xlsx", M)
```

### Lendo Planilhas Excel:
```yvira
let linhas = doc.readExcel("demonstrativo.xlsx")
foreach (linha in linhas) {
    print("Linha:", linha)
}
```

---

## 3. Geração de Relatórios Executivos em PDF (`doc.writePdf`)

Permite renderizar documentos PDF com layout profissional, paginação automática no rodapé, cabeçalhos destacados e tabelas zebradas:

```yvira
import "doc" as doc

let documento = {
    "title": "RELATÓRIO DE AUDITORIA E INTELIGÊNCIA",
    "subtitle": "Emitido pelo núcleo Yvira Engine",
    "content": "Este relatório foi consolidado através de rotinas analíticas nativas. Todos os valores foram computados através do super motor matricial.",
    "headers": ["Cód.", "Ativo", "Valor de Mercado", "Nível de Risco"],
    "rows": [
        ["ATV-01", "Fundo Cripto Quantitativo", "R$ 450.000,00", "Baixo"],
        ["ATV-02", "Cluster de Redes Neurais", "R$ 280.000,00", "Médio"],
        ["ATV-03", "Contratos Futuros de Ouro", "R$ 620.000,00", "Mínimo"]
    ],
    "footer": "Yvira Platform • Document & Report Service"
}

doc.writePdf("relatorio.pdf", documento)
print("Relatório em PDF renderizado com sucesso!")
```

---

## 4. API de Referência do Módulo `doc`

| Método | Argumentos | Retorno | Descrição |
| :--- | :--- | :--- | :--- |
| `doc.writeExcel(caminho, dados)` | `(string, map \| matrix \| list)` | `true` | Gera um arquivo `.xlsx` completo com cabeçalhos e ajuste automático de colunas. |
| `doc.readExcel(caminho, [aba])` | `(string, [string])` | `YviraList` | Lê uma planilha e retorna linhas de dados como listas de valores. |
| `doc.writePdf(caminho, config)` | `(string, map)` | `true` | Renderiza um documento PDF profissional com título, texto, tabela e rodapé paginado. |
