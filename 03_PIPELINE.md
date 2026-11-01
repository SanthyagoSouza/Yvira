1. Lexer (O Leitor) - Vamos começar por aqui
O que faz: Pega o texto bruto do arquivo (ex: let x = 5;) e fatia em peças de Lego chamadas Tokens (LET, IDENTIFIER, EQUALS, NUMBER, SEMICOLON).

Por que é o primeiro: Porque o resto do compilador não entende texto, só entende Tokens.

2. AST (Árvore Sintática Abstrata - O Molde)
O que faz: É apenas um conjunto de classes sem lógica, que representam a estrutura do código. Por exemplo, uma classe IfStatement, uma classe BinaryExpression (para 2 + 2), etc.

Por que é o segundo: Precisamos desses "moldes" prontos para que o próximo passo saiba onde guardar as informações.

3. Parser (O Gramático)
O que faz: Recebe a lista de Tokens do Lexer e tenta encaixar nos moldes da AST. Ele verifica se a gramática faz sentido. Se o cara digitar let = x 5;, o Parser é quem vai gritar: "Erro de sintaxe! Esperava um nome de variável depois do let".

Por que é o terceiro: Ele precisa dos Tokens do Lexer e das classes da AST para trabalhar.

4. Semantic (O Revisor Lógico)
O que faz: Pega a AST perfeitamente montada e verifica as regras de negócio. A variável x foi declarada antes de ser usada? O cara está tentando somar um número com um texto (5 + "olá")?

Por que é o quarto: Não dá para checar tipos e variáveis se o código estiver com erro de sintaxe.

5. VM / Interpretador (O Motor) - O último grande passo
O que faz: Recebe a AST aprovada pelo Semantic e finalmente executa o código, fazendo a matemática acontecer, chamando as funções e imprimindo coisas na tela.

Por que é o último: É o destino final. Só podemos rodar um código que foi lido, validado sintaticamente e checado logicamente.

6. CLI (O Maestro) - Construído em paralelo
O que faz: É o aplicativo do console que você vai chamar rodando beta run meuarquivo.bt. Ele vai apenas orquestrar tudo: ler o arquivo e passar pelo Lexer -> Parser -> Semantic -> VM.