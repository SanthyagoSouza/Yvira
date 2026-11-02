const vscode = require('vscode');

// Dicionário completo de documentação, assinaturas, auto-complete e inteligência do Yvira 2.0
const YVIRA_COMPLETIONS = [
    // ==========================================
    // 1. DIRETIVAS DO SISTEMA NATIVAS (@)
    // ==========================================
    {
        label: '@db',
        kind: vscode.CompletionItemKind.Keyword,
        detail: '@db("uri") - Conexão Universal com Banco de Dados',
        insertText: new vscode.SnippetString('@db("${1:sqlite://app.db}");'),
        doc: 'Inicializa a conexão de banco de dados SQLite padrão.\nExemplos:\n- `@db("loja.db");`\n- `@db("sqlite://:memory:");`'
    },
    {
        label: '@db.alias',
        kind: vscode.CompletionItemKind.Keyword,
        detail: '@db.<alias>("uri") - Multi-Banco de Dados Nomeado',
        insertText: new vscode.SnippetString('@db.${1:analytics}("${2:sqlite://analytics.db}");'),
        doc: 'Registra uma conexão secundária nomeada para arquiteturas multi-database.'
    },
    {
        label: '@exec',
        kind: vscode.CompletionItemKind.Function,
        detail: '@exec("SQL", [params]) - Execução DDL / DML Parametrizada',
        insertText: new vscode.SnippetString('@exec("${1:INSERT INTO tabela (nome) VALUES (?);}", [${2:valor}]);'),
        doc: 'Executa comandos SQL (CREATE TABLE, INSERT, UPDATE, DELETE) com proteção nativa Anti-SQL Injection via parâmetros preparados.'
    },
    {
        label: '@query',
        kind: vscode.CompletionItemKind.Function,
        detail: '@query("SQL", [params]) - Consulta Relacional Segura',
        insertText: new vscode.SnippetString('let ${1:resultados} = @query("${2:SELECT * FROM tabela WHERE id = ?;}", [${3:id}]);'),
        doc: 'Realiza consultas SQL retornando uma lista de dicionários/mapas indexáveis por coluna.'
    },
    {
        label: '@transaction',
        kind: vscode.CompletionItemKind.Keyword,
        detail: '@transaction { ... } - Transação Atômica ACID',
        insertText: new vscode.SnippetString('@transaction {\n\t${1:# operacoes com commit/rollback automatico}\n}'),
        doc: 'Garante atomicidade em operações de banco de dados. Qualquer exceção disparada dentro do bloco executa ROLLBACK imediato.'
    },
    {
        label: '@time',
        kind: vscode.CompletionItemKind.Event,
        detail: '@time.funcao(intervalo, ...args) - Tarefa Recorrente em Segundo Plano',
        insertText: new vscode.SnippetString('@time.${1:minha_funcao}(${2:1.0});'),
        doc: 'Executa uma função periodicamente em background numa threadpool isolada sem travar o programa.'
    },
    {
        label: '@stop',
        kind: vscode.CompletionItemKind.Event,
        detail: '@stop.funcao() - Interrompe Agendamento Temporal',
        insertText: new vscode.SnippetString('@stop.${1:minha_funcao}();'),
        doc: 'Cancela a execução contínua de um timer agendado pelo `@time`.'
    },
    {
        label: '@spawn',
        kind: vscode.CompletionItemKind.Keyword,
        detail: '@spawn { ... } ou @spawn.funcao(...) - Concorrência em Background',
        insertText: new vscode.SnippetString('@spawn {\n\t${1:# codigo executado em thread paralela}\n};'),
        doc: 'Despacha um bloco de código ou função para execução paralela assíncrona.'
    },
    {
        label: '@worker.pool',
        kind: vscode.CompletionItemKind.Keyword,
        detail: '@worker.pool(N) - Paralelismo Multicore Dedicado',
        insertText: new vscode.SnippetString('let pool = @worker.pool(${1:4});'),
        doc: 'Cria uma pool de trabalhadores nativos para processamento massivo distribuído em múltiplos núcleos de CPU.'
    },
    {
        label: '@realtime.server',
        kind: vscode.CompletionItemKind.Module,
        detail: '@realtime.server({ port, secret }) - Servidor WebSocket com E2E',
        insertText: new vscode.SnippetString('let servidor = @realtime.server({\n\t"port": ${1:7080},\n\t"secret": "${2:CHAVE_AES_256}"\n});'),
        doc: 'Inicializa um servidor em tempo real de alta performance com criptografia ponta a ponta e suporte a canais.'
    },
    {
        label: '@realtime.client',
        kind: vscode.CompletionItemKind.Module,
        detail: '@realtime.client({ host, port, secret }) - Cliente Realtime E2E',
        insertText: new vscode.SnippetString('let cliente = @realtime.client({\n\t"host": "${1:127.0.0.1}",\n\t"port": ${2:7080},\n\t"secret": "${3:CHAVE_AES_256}"\n});'),
        doc: 'Conecta a um servidor Yvira Realtime com descriptografia de pacotes em tempo de execução.'
    },
    {
        label: '@pdf.fromView',
        kind: vscode.CompletionItemKind.Function,
        detail: '@pdf.fromView(htmlView, outputPdf, data) - Geração de PDF via HTML',
        insertText: new vscode.SnippetString('@pdf.fromView("${1:views/relatorio.html}", "${2:relatorio.pdf}", ${3:dados});'),
        doc: 'Renderiza um template HTML com CSS e gera um arquivo PDF profissional formatado no disco.'
    },
    {
        label: '@sleep',
        kind: vscode.CompletionItemKind.Function,
        detail: '@sleep(segundos) - Pausa Assíncrona do Processo',
        insertText: new vscode.SnippetString('@sleep(${1:1.0});'),
        doc: 'Pausa a execução do processo sem bloquear a fila de mensagens do sistema.'
    },
    {
        label: '@die',
        kind: vscode.CompletionItemKind.Function,
        detail: '@die(mensagem) - Encerramento Imediato',
        insertText: new vscode.SnippetString('@die("${1:Mensagem de erro fatal}");'),
        doc: 'Interrompe a execução do script instantaneamente com mensagem no stderr.'
    },

    // ==========================================
    // 2. PALAVRAS-CHAVE, CONTROLE E DECLARAÇÕES
    // ==========================================
    {
        label: 'fn',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'fn nome(a, b) { ... } - Declaração de Função',
        insertText: new vscode.SnippetString('fn ${1:nome}(${2:params}) {\n\t${3:# instrucoes}\n\treturn ${4:null};\n}'),
        doc: 'Declara uma função com escopo léxico próprio, retorno opcional e suporte a funções de primeira classe.'
    },
    {
        label: 'class',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'class Nome { fn constructor(...) { ... } } - Classe POO',
        insertText: new vscode.SnippetString('class ${1:NomeClasse} {\n\tfn constructor(${2:params}) {\n\t\tthis.${3:propriedade} = ${2:params};\n\t}\n\n\tfn ${4:metodo}() {\n\t\t${5:# corpo}\n\t}\n}'),
        doc: 'Declara uma classe orientada a objetos com construtor, propriedades e métodos de instância.'
    },
    {
        label: 'class_extends',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'class Filho extends Pai { ... } - Herança POO',
        insertText: new vscode.SnippetString('class ${1:Filho} extends ${2:Pai} {\n\tfn constructor(${3:args}) {\n\t\tthis.${4:campo} = ${3:args};\n\t}\n}'),
        doc: 'Cria uma classe que herda métodos e comportamentos da classe pai.'
    },
    {
        label: 'static_fn',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'static fn metodo(...) { ... } - Método Estático',
        insertText: new vscode.SnippetString('static fn ${1:nomeMetodo}(${2:params}) {\n\t${3:# corpo}\n}'),
        doc: 'Método acessível diretamente pela classe sem necessidade de instanciar com `new`.'
    },
    {
        label: 'let',
        kind: vscode.CompletionItemKind.Keyword,
        detail: 'let variavel = valor; - Variável Mutável',
        insertText: new vscode.SnippetString('let ${1:variavel} = ${2:valor};'),
        doc: 'Declara uma variável mutável com tipagem dinâmica ou anotação opcional (`let x: Int = 10;`).'
    },
    {
        label: 'const',
        kind: vscode.CompletionItemKind.Keyword,
        detail: 'const CONSTANTE = valor; - Constante Imutável',
        insertText: new vscode.SnippetString('const ${1:NOME} = ${2:valor};'),
        doc: 'Declara uma constante cujo valor não pode ser reatribuído após inicializada.'
    },
    {
        label: 'if',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'if (condicao) { ... }',
        insertText: new vscode.SnippetString('if (${1:condicao}) {\n\t${2:# entao}\n}'),
        doc: 'Estrutura condicional if.'
    },
    {
        label: 'if_elif_else',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'if (c1) { ... } elif (c2) { ... } else { ... }',
        insertText: new vscode.SnippetString('if (${1:condicao1}) {\n\t${2:# caso 1}\n} elif (${3:condicao2}) {\n\t${4:# caso 2}\n} else {\n\t${5:# senao}\n}'),
        doc: 'Estrutura condicional completa com elif e else.'
    },
    {
        label: 'match',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'match (expr) { caso => corpo, default => ... }',
        insertText: new vscode.SnippetString('match (${1:expressao}) {\n\t${2:valor1} => {\n\t\t${3:# comandos}\n\t},\n\t${4:valor2} => ${5:resultado},\n\tdefault => {\n\t\t${6:# caso padrao}\n\t}\n}'),
        doc: 'Correspondência de padrões moderna com operador `=>` e wildcard `default` ou `_`.'
    },
    {
        label: 'while',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'while (condicao) { ... } - Laço While',
        insertText: new vscode.SnippetString('while (${1:condicao}) {\n\t${2:# repeticao}\n}'),
        doc: 'Executa repetidamente o bloco enquanto a condição permanecer verdadeira.'
    },
    {
        label: 'for_range',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'for i in range(N) { ... } - Iteração com Range',
        insertText: new vscode.SnippetString('for ${1:i} in range(${2:10}) {\n\t${3:# iteracao}\n}'),
        doc: 'Itera sequencialmente de 0 até o limite especificado em `range`.'
    },
    {
        label: 'for_in',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'for item in colecao { ... } - Iteração de Elementos',
        insertText: new vscode.SnippetString('for ${1:item} in ${2:colecao} {\n\t${3:# iteracao}\n}'),
        doc: 'Itera diretamente sobre os elementos de uma lista, linhas de matriz ou chaves.'
    },
    {
        label: 'try_catch_finally',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'try { ... } catch e { ... } finally { ... }',
        insertText: new vscode.SnippetString('try {\n\t${1:# codigo critico}\n} catch ${2:erro} {\n\tprint("[ERRO]: " + str(${2:erro}));\n} finally {\n\t${3:# limpeza garantida}\n}'),
        doc: 'Estrutura completa de proteção e tratamento de exceções com garantia de execução no `finally`.'
    },
    {
        label: 'throw',
        kind: vscode.CompletionItemKind.Keyword,
        detail: 'throw "mensagem de erro"; - Dispara Exceção',
        insertText: new vscode.SnippetString('throw "${1:Erro critico}";'),
        doc: 'Lança manualmente um erro que interrompe o fluxo normal e salta para o bloco `catch`.'
    },
    {
        label: 'import',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'import "caminho" as alias;',
        insertText: new vscode.SnippetString('import "${1:modulo}" as ${2:alias};'),
        doc: 'Importa arquivos de código Yvira (.yv), variáveis de ambiente (.env), JSON (.json) ou módulos do sistema ("fs", "http", "cache", "crypto", "doc").'
    },

    // ==========================================
    // 3. SUPER MOTOR MATRICIAL E ÁLGEBRA LINEAR
    // ==========================================
    {
        label: 'zeros',
        kind: vscode.CompletionItemKind.Function,
        detail: 'zeros(linhas, colunas) - Gerador de Matriz Nula',
        insertText: new vscode.SnippetString('zeros(${1:linhas}, ${2:colunas})'),
        doc: 'Gera instantaneamente uma matriz bidimensional preenchida com 0.0 sem precisar de loops.'
    },
    {
        label: 'ones',
        kind: vscode.CompletionItemKind.Function,
        detail: 'ones(linhas, colunas) - Gerador de Matriz de Uns',
        insertText: new vscode.SnippetString('ones(${1:linhas}, ${2:colunas})'),
        doc: 'Gera uma matriz com todas as posições preenchidas com 1.0.'
    },
    {
        label: 'eye',
        kind: vscode.CompletionItemKind.Function,
        detail: 'eye(tamanho) - Matriz Identidade Quadrada',
        insertText: new vscode.SnippetString('eye(${1:3})'),
        doc: 'Cria uma Matriz Identidade com diagonal principal igual a 1.0 e demais células iguais a 0.0.'
    },
    {
        label: 'randMat',
        kind: vscode.CompletionItemKind.Function,
        detail: 'randMat(linhas, colunas, min, max) - Matriz Aleatória',
        insertText: new vscode.SnippetString('randMat(${1:linhas}, ${2:colunas}, ${3:0.0}, ${4:10.0})'),
        doc: 'Cria uma matriz preenchida com números aleatórios dentro do intervalo configurado.'
    },
    {
        label: 'solve',
        kind: vscode.CompletionItemKind.Function,
        detail: 'solve(A, b) - Resolve Sistema de Equações Lineares (A · x = b)',
        insertText: new vscode.SnippetString('let x = solve(${1:A}, ${2:b});'),
        doc: 'Resolve sistemas de equações lineares de física/engenharia através de multiplicação pela matriz inversa ($x = A^{-1} \\cdot b$).'
    },
    {
        label: 'inverse',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.inverse() / .inv() - Inversão de Gauss-Jordan',
        insertText: new vscode.SnippetString('inverse()'),
        doc: 'Inverte uma matriz quadrada com pivotamento parcial garantindo precisão matemática.'
    },
    {
        label: 'transpose',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.transpose() / .T - Matriz Transposta',
        insertText: new vscode.SnippetString('transpose()'),
        doc: 'Inverte linhas por colunas da matriz instantaneamente.'
    },
    {
        label: 'det',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.det() - Determinante Escalar',
        insertText: new vscode.SnippetString('det()'),
        doc: 'Calcula o determinante escalar exato usando decomposição triangular/gaussiana.'
    },
    {
        label: 'trace',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.trace() - Traço da Diagonal Principal',
        insertText: new vscode.SnippetString('trace()'),
        doc: 'Calcula a soma dos elementos da diagonal principal de uma matriz quadrada.'
    },
    {
        label: 'row',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.row(indice) - Fatiamento de Linha',
        insertText: new vscode.SnippetString('row(${1:0})'),
        doc: 'Extrai uma linha inteira da matriz como novo vetor 1D sem laço for.'
    },
    {
        label: 'col',
        kind: vscode.CompletionItemKind.Method,
        detail: 'matriz.col(indice) - Fatiamento de Coluna',
        insertText: new vscode.SnippetString('col(${1:0})'),
        doc: 'Extrai uma coluna inteira da matriz como novo vetor coluna sem laço for.'
    },

    // ==========================================
    // 4. MOTOR DESKTOP DE JANELAS (WEBVIEW2)
    // ==========================================
    {
        label: 'create_window',
        kind: vscode.CompletionItemKind.Snippet,
        detail: 'create_window({ ... }) - Cria Janela Desktop Nativa',
        insertText: new vscode.SnippetString('let app = create_window({\n\t"title": "${1:Meu App Yvira}",\n\t"view": "${2:views/index.html}",\n\t"width": ${3:1200},\n\t"height": ${4:750},\n\t"resizable": true,\n\t"custom_titlebar": true,\n\t"window_mode": "normal"\n});\n\napp.show();\napp.wait();'),
        doc: 'Inicializa uma janela desktop moderna integrada via Microsoft WebView2 com aceleração de hardware e ponte de dados DOM bidirecional.'
    },
    {
        label: 'app.emit',
        kind: vscode.CompletionItemKind.Method,
        detail: 'app.emit(canal, dados) - Envia Dados para o Front-end',
        insertText: new vscode.SnippetString('app.emit("${1:canal_evento}", ${2:dados});'),
        doc: 'Injeta dados nativos do Yvira diretamente no JavaScript do front-end sem passar por HTTP/JSON.'
    },
    {
        label: 'app.on',
        kind: vscode.CompletionItemKind.Method,
        detail: 'app.on(canal, callback) - Ouve Eventos do Front-end',
        insertText: new vscode.SnippetString('app.on("${1:canal_evento}", fn(dados) {\n\t${2:# processa dados recebidos da tela}\n});'),
        doc: 'Registra um listener para capturar cliques ou ações enviadas pelo `window.yvira.emit()` do navegador.'
    },

    // ==========================================
    // 5. TUI E TERMINAL RICO
    // ==========================================
    {
        label: 'menu',
        kind: vscode.CompletionItemKind.Function,
        detail: 'menu(titulo, opcoes) - Menu Interativo com Setas',
        insertText: new vscode.SnippetString('let ${1:escolha} = menu("${2:Titulo do Menu}", [${3:"Opcao 1", "Opcao 2"}]);'),
        doc: 'Exibe um menu navegável via setas do teclado (↑ / ↓) e Enter, retornando o índice numérico da opção selecionada.'
    },
    {
        label: 'box',
        kind: vscode.CompletionItemKind.Function,
        detail: 'box(titulo, conteudo) - Caixa Estilizada Unicode',
        insertText: new vscode.SnippetString('box("${1:TITULO}", "${2:Mensagem interna}");'),
        doc: 'Desenha no terminal uma caixa com bordas duplas Unicode e cabeçalho formatado.'
    },
    {
        label: 'progress_bar',
        kind: vscode.CompletionItemKind.Function,
        detail: 'progress_bar(atual, total, rotulo) - Barra de Progresso Animada',
        insertText: new vscode.SnippetString('progress_bar(${1:progresso}, ${2:100}, "${3:Processando:}");'),
        doc: 'Desenha e anima uma barra de progresso no terminal com porcentagem calculada automaticamente.'
    },
    {
        label: 'confirm',
        kind: vscode.CompletionItemKind.Function,
        detail: 'confirm(prompt) - Pergunta Sim/Não Interativa',
        insertText: new vscode.SnippetString('if (confirm("${1:Deseja continuar?}")) {\n\t${2:# acao aprovada}\n}'),
        doc: 'Solicita confirmação do usuário via teclado (S/N) e retorna `true` ou `false`.'
    },

    // ==========================================
    // 6. BUILT-INS DA BIBLIOTECA PADRÃO
    // ==========================================
    {
        label: 'print',
        kind: vscode.CompletionItemKind.Function,
        detail: 'print(...) - Imprime no Console',
        insertText: new vscode.SnippetString('print(${1:mensagem});'),
        doc: 'Imprime valores ou variáveis formatados no terminal com quebra de linha.'
    },
    {
        label: 'input',
        kind: vscode.CompletionItemKind.Function,
        detail: 'input(prompt) - Leitura do Teclado',
        insertText: new vscode.SnippetString('let ${1:dado} = input("${2:Digite: }");'),
        doc: 'Solicita a digitação de uma linha pelo usuário no console.'
    },
    {
        label: 'toNumber',
        kind: vscode.CompletionItemKind.Function,
        detail: 'toNumber(valor) - Converte para Número Decimal',
        insertText: new vscode.SnippetString('toNumber(${1:texto})'),
        doc: 'Converte uma String em número de ponto flutuante, aceitando tanto ponto quanto vírgula decimal.'
    },
    {
        label: 'toInt',
        kind: vscode.CompletionItemKind.Function,
        detail: 'toInt(valor) - Trunca para Inteiro',
        insertText: new vscode.SnippetString('toInt(${1:valor})'),
        doc: 'Retorna a parte inteira truncada de um número decimal.'
    },
    {
        label: 'toString',
        kind: vscode.CompletionItemKind.Function,
        detail: 'toString(valor) - Converte para Texto',
        insertText: new vscode.SnippetString('toString(${1:valor})'),
        doc: 'Converte qualquer valor, mapa ou lista em sua representação textual.'
    },
    {
        label: 'random',
        kind: vscode.CompletionItemKind.Function,
        detail: 'random(min, max) - Número Aleatório',
        insertText: new vscode.SnippetString('random(${1:1}, ${2:100})'),
        doc: 'Gera um número pseudo-aleatório inteiro entre min e max.'
    },
    {
        label: 'clock',
        kind: vscode.CompletionItemKind.Function,
        detail: 'clock() - Timestamp em Milissegundos',
        insertText: new vscode.SnippetString('clock()'),
        doc: 'Retorna o timestamp atual em milissegundos Unix.'
    },
    {
        label: 'clear',
        kind: vscode.CompletionItemKind.Function,
        detail: 'clear() - Limpa a Tela do Terminal',
        insertText: new vscode.SnippetString('clear();'),
        doc: 'Limpa o buffer de tela do console.'
    },
    {
        label: 'sleep',
        kind: vscode.CompletionItemKind.Function,
        detail: 'sleep(ms) - Pausa a Thread',
        insertText: new vscode.SnippetString('sleep(${1:1000});'),
        doc: 'Pausa a execução da thread atual por N milissegundos.'
    }
];

function activate(context) {
    const diagnosticCollection = vscode.languages.createDiagnosticCollection('yvira');
    context.subscriptions.push(diagnosticCollection);

    // 1. Diagnósticos e Validador de Delimitadores em Tempo Real
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

    // 2. Provedor de Auto-Complete Inteligente
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
    }, '@', '.', '(', '"', '$');
    context.subscriptions.push(completionProvider);

    // 3. Provedor de Hover Tooltips Ricos
    const hoverProvider = vscode.languages.registerHoverProvider('yvira', {
        provideHover(document, position) {
            const range = document.getWordRangeAtPosition(position, /[@a-zA-Z_0-9\.]+/);
            if (!range) return null;
            const word = document.getText(range);

            const found = YVIRA_COMPLETIONS.find(c => c.label === word || c.label.replace('@', '') === word || c.label.split('.').pop() === word);
            if (found) {
                return new vscode.Hover(new vscode.MarkdownString(`### ${found.detail}\n\n${found.doc}`));
            }
            return null;
        }
    });
    context.subscriptions.push(hoverProvider);

    // 4. Provedor de Formatação de Documento (DocumentFormattingEditProvider)
    const formattingProvider = vscode.languages.registerDocumentFormattingEditProvider('yvira', {
        provideDocumentFormattingEdits(document, options) {
            const text = document.getText();
            const formatted = formatYviraCode(text, options.tabSize || 4);
            const fullRange = new vscode.Range(
                document.positionAt(0),
                document.positionAt(text.length)
            );
            return [vscode.TextEdit.replace(fullRange, formatted)];
        }
    });
    context.subscriptions.push(formattingProvider);
}

// Algoritmo nativo de formatação do Yvira integrado na extensão
function formatYviraCode(sourceCode, indentSpaces = 4) {
    if (!sourceCode || sourceCode.trim().length === 0) return '';

    // 1. Quebra declarações múltiplas na mesma linha preservando strings e comentários
    let code = '';
    let inString = false;
    let inInterp = false;

    for (let i = 0; i < sourceCode.length; i++) {
        const c = sourceCode[i];

        if (c === '\\' && i + 1 < sourceCode.length && (inString || inInterp)) {
            code += c + sourceCode[++i];
            continue;
        }

        if (!inString && !inInterp) {
            if (c === '#') {
                while (i < sourceCode.length && sourceCode[i] !== '\n') {
                    code += sourceCode[i++];
                }
                if (i < sourceCode.length) code += sourceCode[i];
                continue;
            }

            if (c === '$' && i + 1 < sourceCode.length && sourceCode[i + 1] === '"') {
                inInterp = true;
                code += c + sourceCode[++i];
                continue;
            }

            if (c === '"') {
                inString = true;
                code += c;
                continue;
            }

            code += c;

            if (c === ';' || c === '{') {
                let next = i + 1;
                while (next < sourceCode.length && (sourceCode[next] === ' ' || sourceCode[next] === '\t')) {
                    next++;
                }
                if (next < sourceCode.length && sourceCode[next] !== '\r' && sourceCode[next] !== '\n' && sourceCode[next] !== '#') {
                    code += '\n';
                }
            }
        } else if (inString) {
            code += c;
            if (c === '"') inString = false;
        } else if (inInterp) {
            code += c;
            if (c === '"') inInterp = false;
        }
    }

    // 2. Normaliza e indenta linha a linha
    const rawLines = code.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n');
    const formattedLines = [];
    let indentLevel = 0;
    const indentStr = ' '.repeat(indentSpaces);

    for (let rawLine of rawLines) {
        let line = rawLine.trim();

        if (line.length === 0) {
            if (formattedLines.length > 0 && formattedLines[formattedLines.length - 1] !== '') {
                formattedLines.push('');
            }
            continue;
        }

        if (line.startsWith('#') && !line.startsWith('#-')) {
            formattedLines.push(indentStr.repeat(indentLevel) + line);
            continue;
        }

        let leadingClosers = 0;
        for (let ch of line) {
            if (ch === '}' || ch === ']' || ch === ')') leadingClosers++;
            else if (ch === ' ' || ch === '\t') continue;
            else break;
        }

        let effectiveIndent = Math.max(0, indentLevel - leadingClosers);
        let formattedLineText = formatLineTokens(line);

        formattedLines.push(indentStr.repeat(effectiveIndent) + formattedLineText);

        let delta = 0;
        let sIn = false, iIn = false;
        for (let j = 0; j < line.length; j++) {
            let ch = line[j];
            if (ch === '\\' && j + 1 < line.length && (sIn || iIn)) { j++; continue; }
            if (!sIn && !iIn) {
                if (ch === '#') break;
                if (ch === '$' && j + 1 < line.length && line[j + 1] === '"') { iIn = true; j++; continue; }
                if (ch === '"') { sIn = true; continue; }
                if (ch === '{' || ch === '[' || ch === '(') delta++;
                else if (ch === '}' || ch === ']' || ch === ')') delta--;
            } else if (sIn && ch === '"') sIn = false;
            else if (iIn && ch === '"') iIn = false;
        }

        indentLevel = Math.max(0, indentLevel + delta);
    }

    return formattedLines.join('\n') + '\n';
}

function formatLineTokens(line) {
    let sb = '';
    let inString = false;
    let inInterp = false;

    for (let i = 0; i < line.length; i++) {
        let c = line[i];

        if (c === '\\' && i + 1 < line.length && (inString || inInterp)) {
            sb += c + line[++i];
            continue;
        }

        if (!inString && !inInterp) {
            if (c === '#') {
                if (sb.length > 0 && !sb.endsWith(' ')) sb += ' ';
                sb += line.substring(i);
                break;
            }

            if (c === '$' && i + 1 < line.length && line[i + 1] === '"') {
                inInterp = true;
                sb += '$"';
                i++;
                continue;
            }

            if (c === '"') {
                inString = true;
                sb += c;
                continue;
            }

            if (c === '{') {
                if (sb.length > 0 && !sb.endsWith(' ') && !sb.endsWith('(') && !sb.endsWith('[')) {
                    sb += ' ';
                }
                sb += '{';
                continue;
            }

            if (c === ',' || c === ';') {
                sb = sb.replace(/\s+$/, '');
                sb += c;
                if (i + 1 < line.length && line[i + 1] !== ' ' && line[i + 1] !== ')' && line[i + 1] !== ']' && line[i + 1] !== '}') {
                    sb += ' ';
                }
                continue;
            }

            if (c === ':') {
                sb = sb.replace(/\s+$/, '');
                sb += ':';
                if (i + 1 < line.length && line[i + 1] !== ' ') sb += ' ';
                continue;
            }

            if (c === '=' && i + 1 < line.length && line[i + 1] === '>') {
                if (sb.length > 0 && !sb.endsWith(' ')) sb += ' ';
                sb += '=>';
                i++;
                if (i + 1 < line.length && line[i + 1] !== ' ') sb += ' ';
                continue;
            }

            if (i + 1 < line.length &&
                ((c === '=' && line[i + 1] === '=') ||
                 (c === '!' && line[i + 1] === '=') ||
                 (c === '<' && line[i + 1] === '=') ||
                 (c === '>' && line[i + 1] === '=') ||
                 (c === '+' && line[i + 1] === '=') ||
                 (c === '-' && line[i + 1] === '=') ||
                 (c === '*' && line[i + 1] === '=') ||
                 (c === '/' && line[i + 1] === '=') ||
                 (c === '*' && line[i + 1] === '*'))) {
                if (sb.length > 0 && !sb.endsWith(' ')) sb += ' ';
                sb += c + line[i + 1];
                i++;
                if (i + 1 < line.length && line[i + 1] !== ' ') sb += ' ';
                continue;
            }

            if ((c === '=' && (i + 1 >= line.length || (line[i + 1] !== '=' && line[i + 1] !== '>'))) ||
                (c === '<' && (i + 1 >= line.length || (line[i + 1] !== '=' && line[i + 1] !== '<'))) ||
                (c === '>' && (i + 1 >= line.length || (line[i + 1] !== '=' && line[i + 1] !== '>')))) {
                if (sb.length > 0 && !sb.endsWith(' ')) sb += ' ';
                sb += c;
                if (i + 1 < line.length && line[i + 1] !== ' ') sb += ' ';
                continue;
            }

            if (c === '+' || c === '-' || c === '*' || c === '/') {
                let isUnary = (c === '-' || c === '+') && (sb.length === 0 || sb.endsWith('(') || sb.endsWith('[') || sb.endsWith(',') || sb.endsWith('='));
                if (!isUnary) {
                    if (sb.length > 0 && !sb.endsWith(' ')) sb += ' ';
                    sb += c;
                    if (i + 1 < line.length && line[i + 1] !== ' ') sb += ' ';
                    continue;
                }
            }

            sb += c;
        } else if (inString) {
            sb += c;
            if (c === '"') inString = false;
        } else if (inInterp) {
            sb += c;
            if (c === '"') inInterp = false;
        }
    }

    return sb.trimEnd().replace(/\}else/g, '} else').replace(/\}elif/g, '} elif');
}
