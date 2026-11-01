const vscode = require('vscode');

// Dicionário completo de documentação, assinaturas e auto-complete do Yvira
const YVIRA_COMPLETIONS = [
    // 1. DIRETIVAS NATIVAS (@)
    {
        label: '@db',
        kind: vscode.CompletionItemKind.Keyword,
        detail: '@db("uri") - Conexão Universal com Banco de Dados',
        insertText: new vscode.SnippetString('@db("${1:sqlite://app.db}");'),
        doc: 'Inicializa a conexão de banco de dados padrão.\nExemplos:\n- `@db("sqlite://banco.db")`\n- `@db("sqlite://:memory:")`'
    },
    {
        label: '@db.alias',
        kind: vscode.CompletionItemKind.Keyword,
        detail: '@db.<alias>("uri") - Multi-Banco de Dados Nomeado',
        insertText: new vscode.SnippetString('@db.${1:analytics}("${2:sqlite://analytics.db}");'),
        doc: 'Registra uma conexão de banco de dados secundária com nome/alias específico para multi-bancos no mesmo script.'
    },
    {
        label: '@exec',
        kind: vscode.CompletionItemKind.Function,
        detail: '@exec("SQL", [params]) - Execução DDL / DML Segura',
        insertText: new vscode.SnippetString('@exec("${1:INSERT INTO tabela (coluna) VALUES (?);}", [${2:valor}]);'),
        doc: 'Executa comandos SQL (CREATE TABLE, INSERT, UPDATE, DELETE) com proteção nativa anti-SQL injection via parâmetros preparados.'
    },
    {
        label: '@exec.alias',
        kind: vscode.CompletionItemKind.Function,
        detail: '@exec.<alias>("SQL", [params]) - Execução em Banco Nomeado',
        insertText: new vscode.SnippetString('@exec.${1:analytics}("${2:INSERT INTO logs (msg) VALUES (?);}", [${3:valor}]);'),
        doc: 'Executa comandos DDL/DML em uma conexão de banco de dados específica.'
    },
    {
        label: '@query',
        kind: vscode.CompletionItemKind.Function,
        detail: '@query("SQL", [params]) - Consulta SQL Segura',
        insertText: new vscode.SnippetString('let ${1:resultados} = @query("${2:SELECT * FROM tabela WHERE id = ?;}", [${3:id}]);'),
        doc: 'Realiza consultas SQL parametrizadas retornando uma lista de dicionários/mapas indexáveis por nome de coluna.'
    },
    {
        label: '@query.alias',
        kind: vscode.CompletionItemKind.Function,
        detail: '@query.<alias>("SQL", [params]) - Consulta em Banco Nomeado',
        insertText: new vscode.SnippetString('let ${1:resultados} = @query.${2:analytics}("${3:SELECT * FROM logs WHERE data = ?;}", [${4:data}]);'),
        doc: 'Realiza consultas SQL em um banco de dados específico retornando mapas de registros.'
    },
    {
        label: '@time',
        kind: vscode.CompletionItemKind.Event,
        detail: '@time.funcao(intervalo, ...args) - Automação sem Cron',
        insertText: new vscode.SnippetString('@time.${1:minha_funcao}(${2:intervalo_segundos});'),
        doc: 'Ativa uma função como agendamento temporal periódico em background sem precisar de cron ou daemons de sistema operacional.'
    },
    {
        label: '@stop',
        kind: vscode.CompletionItemKind.Event,
        detail: '@stop.funcao() - Interrompe Automação Temporal',
        insertText: new vscode.SnippetString('@stop.${1:minha_funcao}();'),
        doc: 'Interrompe a execução agendada da função especificada.'
    },

    // 2. PALAVRAS-CHAVE E DECLARAÇÕES
    {
        label: 'fn',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'fn nome(params) { ... } - Declaração de Função',
        insertText: new vscode.SnippetString('fn ${1:nome}(${2:params}) {\n\t${3:# corpo}\n\treturn ${4:null};\n}'),
        doc: 'Declara uma nova função com parâmetros e escopo léxico próprio.'
    },
    {
        label: 'class',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'class Nome { fn init(...) { ... } } - Declaração de Classe',
        insertText: new vscode.SnippetString('class ${1:NomeClasse} {\n\tfn init(${2:args}) {\n\t\tthis.${3:prop} = ${2:args};\n\t}\n}'),
        doc: 'Declara uma classe com construtor `fn init` e métodos orientados a objetos.'
    },
    {
        label: 'let',
        kind: vscode.CompletionItemKind.Keyword,
        detail: 'let variavel = valor; - Variável Mutável',
        insertText: new vscode.SnippetString('let ${1:variavel} = ${2:valor};'),
        doc: 'Declara uma variável com escopo local mutável.'
    },
    {
        label: 'const',
        kind: vscode.CompletionItemKind.Keyword,
        detail: 'const CONSTANTE = valor; - Constante Imutável',
        insertText: new vscode.SnippetString('const ${1:NOME} = ${2:valor};'),
        doc: 'Declara uma constante cujo valor não pode ser reatribuído.'
    },
    {
        label: 'if',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'if (condicao) { ... } - Controle Condicional',
        insertText: new vscode.SnippetString('if (${1:condicao}) {\n\t${2:# codigo}\n}'),
        doc: 'Estrutura condicional if.'
    },
    {
        label: 'ifelse',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'if (condicao) { ... } else { ... }',
        insertText: new vscode.SnippetString('if (${1:condicao}) {\n\t${2:# entao}\n} else {\n\t${3:# senao}\n}'),
        doc: 'Estrutura condicional if-else.'
    },
    {
        label: 'foreach',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'foreach (item in lista) { ... } - Iterador de Coleções',
        insertText: new vscode.SnippetString('foreach (${1:item} in ${2:colecao}) {\n\tprint(${1:item});\n}'),
        doc: 'Itera sobre elementos de listas, mapas ou matrizes.'
    },
    {
        label: 'while',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'while (condicao) { ... } - Laço de Repetição',
        insertText: new vscode.SnippetString('while (${1:condicao}) {\n\t${2:# repeticao}\n}'),
        doc: 'Executa o bloco enquanto a condição for verdadeira.'
    },
    {
        label: 'for',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'for (let i = 0; i < N; i = i + 1) { ... }',
        insertText: new vscode.SnippetString('for (let ${1:i} = 0; ${1:i} < ${2:limite}; ${1:i} = ${1:i} + 1) {\n\t${3:# iteracao}\n}'),
        doc: 'Laço de repetição for com inicialização, condição e passo.'
    },
    {
        label: 'trycatch',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'try { ... } catch (e) { ... } - Tratamento de Erros',
        insertText: new vscode.SnippetString('try {\n\t${1:# codigo protegido}\n} catch (${2:erro}) {\n\tprint("[FALHA]: " + ${2:erro});\n}'),
        doc: 'Captura exceções lançadas durante a execução.'
    },
    {
        label: 'import',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'import "caminho" as alias;',
        insertText: new vscode.SnippetString('import "${1:modulo}" as ${2:alias};'),
        doc: 'Importa módulos Yvira (.yv), variáveis de ambiente (.env), arquivos JSON (.json) ou módulos nativos ("fs", "http").'
    },

    // 3. MÓDULOS NATIVOS E MÉTODOS
    {
        label: 'http.get',
        kind: vscode.CompletionItemKind.Method,
        detail: 'http.get(url) - Requisição HTTP GET',
        insertText: new vscode.SnippetString('let ${1:res} = http.get("${2:https://api.exemplo.com/dados}");\nlet ${3:dados} = ${1:res}.json();'),
        doc: 'Realiza uma requisição HTTP GET de alta performance e permite acessar `.status`, `.body` e `.json()`.'
    },
    {
        label: 'http.post',
        kind: vscode.CompletionItemKind.Method,
        detail: 'http.post(url, payload) - Requisição HTTP POST',
        insertText: new vscode.SnippetString('let ${1:res} = http.post("${2:https://api.exemplo.com}", ${3:dados});'),
        doc: 'Envia dados via HTTP POST com serialização automática de corpo.'
    },
    {
        label: 'fs.read',
        kind: vscode.CompletionItemKind.Method,
        detail: 'fs.read(path) - Leitura de Arquivo Texto',
        insertText: new vscode.SnippetString('let ${1:conteudo} = fs.read("${2:arquivo.txt}");'),
        doc: 'Lê o conteúdo textual completo de um arquivo no disco.'
    },
    {
        label: 'fs.write',
        kind: vscode.CompletionItemKind.Method,
        detail: 'fs.write(path, content) - Escrita em Arquivo',
        insertText: new vscode.SnippetString('fs.write("${1:arquivo.txt}", ${2:conteudo});'),
        doc: 'Sobrescreve ou cria um arquivo com o conteúdo especificado.'
    },
    {
        label: 'fs.exists',
        kind: vscode.CompletionItemKind.Method,
        detail: 'fs.exists(path) - Checa Existência de Arquivo',
        insertText: new vscode.SnippetString('if (fs.exists("${1:caminho}")) {\n\t${2:# arquivo existe}\n}'),
        doc: 'Verifica se o arquivo ou diretório existe no disco.'
    },

    // 4. MÉTODOS DE MATRIZ E CIENTÍFICOS
    {
        label: 'transposta',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.transposta() / .T - Matriz Transposta',
        insertText: new vscode.SnippetString('transposta()'),
        doc: 'Inverte as linhas e colunas da matriz ($A^T$).'
    },
    {
        label: 'inv',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.inv() - Inversão Matricial',
        insertText: new vscode.SnippetString('inv()'),
        doc: 'Calcula a matriz inversa ($A^{-1}$) usando eliminação gaussiana com pivotamento parcial.'
    },
    {
        label: 'det',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.det() - Determinante Escalar',
        insertText: new vscode.SnippetString('det()'),
        doc: 'Calcula o determinante escalar de uma matriz quadrada.'
    },
    {
        label: 'trace',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.trace() - Traço da Matriz',
        insertText: new vscode.SnippetString('trace()'),
        doc: 'Calcula a soma dos elementos da diagonal principal da matriz.'
    },
    {
        label: 'length',
        kind: vscode.CompletionItemKind.Method,
        detail: 'lista.length() - Tamanho da Coleção',
        insertText: new vscode.SnippetString('length()'),
        doc: 'Retorna a quantidade de elementos de uma lista ou consulta.'
    },

    // 5. FUNÇÕES BUILT-IN
    {
        label: 'print',
        kind: vscode.CompletionItemKind.Function,
        detail: 'print(msg) - Exibe no Console',
        insertText: new vscode.SnippetString('print(${1:msg});'),
        doc: 'Imprime uma mensagem ou valor no console de saída.'
    },
    {
        label: 'input',
        kind: vscode.CompletionItemKind.Function,
        detail: 'input(prompt) - Entrada do Usuário',
        insertText: new vscode.SnippetString('let ${1:entrada} = input("${2:Digite: }");'),
        doc: 'Solicita entrada de texto do usuário via teclado.'
    }
];

function activate(context) {
    const diagnosticCollection = vscode.languages.createDiagnosticCollection('yvira');
    context.subscriptions.push(diagnosticCollection);

    // 1. Diagnósticos de Delimitadores em Tempo Real
    function updateDiagnostics(document) {
        if (document.languageId !== 'yvira') return;

        const text = document.getText();
        const diagnostics = [];
        const stack = [];

        for (let i = 0; i < text.length; i++) {
            const ch = text[i];
            if (ch === '(' || ch === '[' || ch === '{') {
                stack.push({ char: ch, pos: document.positionAt(i) });
            } else if (ch === ')' || ch === ']' || ch === '}') {
                if (stack.length === 0) {
                    const range = new vscode.Range(document.positionAt(i), document.positionAt(i + 1));
                    diagnostics.push(new vscode.Diagnostic(range, `Caractere de fechamento inesperado '${ch}'`, vscode.DiagnosticSeverity.Error));
                } else {
                    const open = stack.pop();
                    const expected = (open.char === '(' && ch === ')') || (open.char === '[' && ch === ']') || (open.char === '{' && ch === '}');
                    if (!expected) {
                        const range = new vscode.Range(document.positionAt(i), document.positionAt(i + 1));
                        diagnostics.push(new vscode.Diagnostic(range, `Esperava fechamento compatível para '${open.char}', mas encontrou '${ch}'`, vscode.DiagnosticSeverity.Error));
                    }
                }
            }
        }

        while (stack.length > 0) {
            const unclosed = stack.pop();
            const range = new vscode.Range(unclosed.pos, new vscode.Position(unclosed.pos.line, unclosed.pos.character + 1));
            diagnostics.push(new vscode.Diagnostic(range, `Delimitador não fechado: '${unclosed.char}'`, vscode.DiagnosticSeverity.Error));
        }

        diagnosticCollection.set(document.uri, diagnostics);
    }

    if (vscode.window.activeTextEditor) {
        updateDiagnostics(vscode.window.activeTextEditor.document);
    }
    context.subscriptions.push(vscode.workspace.onDidChangeTextDocument(e => updateDiagnostics(e.document)));
    context.subscriptions.push(vscode.workspace.onDidOpenTextDocument(doc => updateDiagnostics(doc)));

    // 2. Provedor de Auto-Complete Turbinado
    const completionProvider = vscode.languages.registerCompletionItemProvider('yvira', {
        provideCompletionItems(document, position) {
            const items = [];

            for (const itemDef of YVIRA_COMPLETIONS) {
                const item = new vscode.CompletionItem(itemDef.label, itemDef.kind);
                item.detail = itemDef.detail;
                item.insertText = itemDef.insertText;
                item.documentation = new vscode.MarkdownString(itemDef.doc);
                items.push(item);
            }

            return items;
        }
    }, '@', '.', '(', '"');
    context.subscriptions.push(completionProvider);

    // 3. Provedor de Hover Tooltips
    const hoverProvider = vscode.languages.registerHoverProvider('yvira', {
        provideHover(document, position) {
            const range = document.getWordRangeAtPosition(position, /[@a-zA-Z_0-9\.]+/);
            if (!range) return null;
            const word = document.getText(range);

            const found = YVIRA_COMPLETIONS.find(c => c.label === word || c.label.replace('@', '') === word);
            if (found) {
                return new vscode.Hover(new vscode.MarkdownString(`**${found.detail}**\n\n${found.doc}`));
            }
            return null;
        }
    });
    context.subscriptions.push(hoverProvider);
}

function deactivate() {}

module.exports = {
    activate,
    deactivate
};
