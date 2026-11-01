# 17 - Módulos Nativos do Sistema: Arquivos (`fs`) e Rede (`http`)

A biblioteca padrão da linguagem **Yvira** agora conta com dois módulos nativos de alto desempenho: `fs` (File System) e `http` (Networking / REST APIs).

---

## 1. Módulo `fs` (Sistema de Arquivos)

Permite ler, gravar, anexar arquivos em disco e serializar matrizes científicas diretamente para CSV.

```yvira
import "fs" as fs

# 1. Escrita e leitura de texto
fs.write("notas.txt", "Primeira linha de anotacoes\n")
fs.append("notas.txt", "Segunda linha adicionada via append\n")

if (fs.exists("notas.txt")) {
    let conteudo = fs.read("notas.txt")
    print(conteudo)
}

# 2. Leitura linha a linha (retorna uma Lista)
let linhas = fs.readLines("notas.txt")
foreach (linha in linhas) {
    print("Linha:", linha)
}

# 3. Exportação e Importação de Matrizes (.csv)
let A = [[1.0, 2.0], [3.0, 4.0]]
fs.writeCsv("dados.csv", A)

let A_carregada = fs.readCsv("dados.csv")
print("Matriz invertida:")
print(A_carregada.inverse())
```

### Funções do `fs`:
- `fs.read(caminho)`: Retorna o conteúdo textual do arquivo.
- `fs.readLines(caminho)`: Retorna uma lista de strings com as linhas.
- `fs.write(caminho, conteudo)`: Cria ou sobrescreve o arquivo.
- `fs.append(caminho, conteudo)`: Adiciona texto ao final do arquivo.
- `fs.exists(caminho)`: Verifica se arquivo ou diretório existe.
- `fs.remove(caminho)`: Deleta arquivo ou pasta recursivamente.
- `fs.list([dir])`: Lista os arquivos e subpastas de um diretório.
- `fs.writeCsv(caminho, matriz)`: Salva uma matriz em formato CSV.
- `fs.readCsv(caminho)`: Lê um arquivo CSV e retorna um objeto Matrix nativo.

---

## 2. Módulo `http` (Requisições Web e APIs REST)

Permite consumir serviços web e enviar dados via HTTP sem qualquer dependência externa.

```yvira
import "http" as http

try {
    # 1. Requisição GET
    let resposta = http.get("https://jsonplaceholder.typicode.com/todos/1")
    
    print("Status:", resposta.status) # 200
    print("Ok?:", resposta.ok)         # true

    # Converte a resposta JSON automaticamente para Map/List nativo do Yvira:
    let dados = resposta.json()
    print("ID:", dados.id)
    print("Título:", dados.title)
    print("Concluído?:", dados.completed)

    # 2. Requisição POST
    let novoItem = {
        "title": "Aprender Yvira",
        "completed": false
    }
    let postRes = http.post("https://jsonplaceholder.typicode.com/todos", novoItem)
    print("Status do envio:", postRes.status)
    print("Resposta:", postRes.json())

} catch (erro) {
    print("Falha na comunicação de rede:", erro)
}
```