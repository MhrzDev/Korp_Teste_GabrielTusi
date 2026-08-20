# Roteiro do vídeo de apresentação

Tempo recomendado: 6 a 8 minutos.

## 1. Introdução - 30 segundos

“Olá, meu nome é Gabriel Tusi. Esta é a minha solução para o desafio técnico de emissão de notas fiscais. O projeto utiliza Angular no front-end, C# com ASP.NET Core em dois microsserviços, PostgreSQL e Docker.”

## 2. Arquitetura - 60 segundos

Mostre o diagrama do README e explique:

- Interface entregue pelo Nginx.
- Serviço de Estoque responsável por produtos e saldos.
- Serviço de Faturamento responsável pelas notas.
- Bancos separados e comunicação por API.

## 3. Cadastro de produtos - 60 segundos

- Abra Produtos.
- Cadastre dois produtos.
- Mostre validações e saldo.
- Edite um produto brevemente.

## 4. Criação da nota - 60 segundos

- Abra Notas fiscais.
- Crie uma nota com os dois produtos.
- Destaque a numeração sequencial, múltiplos itens e status Aberta.

## 5. Impressão e estoque - 60 segundos

- Clique em Imprimir.
- Mostre o indicador de processamento.
- Mostre a nota Fechada e o botão bloqueado.
- Volte aos produtos e confirme a baixa dos saldos.

## 6. Tratamento de falha - 60 segundos

- Crie outra nota.
- Ative o Modo de demonstração de falha.
- Tente imprimir e mostre a mensagem amigável.
- Confirme que a nota segue Aberta e o estoque não mudou.
- Desative o modo e conclua a impressão.

## 7. Detalhamento técnico - 90 segundos

No editor, mostre rapidamente:

- `ngOnInit`, `forkJoin`, `finalize` e o interceptor do Angular.
- A transação e o `FOR UPDATE` no `StockService`.
- A chave idempotente `invoice-{id}`.
- O cliente HTTP com novas tentativas.
- Os DTOs, LINQ, middleware de exceções e testes xUnit.

## 8. Encerramento - 20 segundos

“A solução atende aos requisitos obrigatórios e inclui concorrência, idempotência, documentação, testes e automação de CI. Obrigado pela oportunidade.”
