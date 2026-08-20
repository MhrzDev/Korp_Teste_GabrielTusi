# Detalhamento técnico - NotaFlow

## 1. Visão geral

A solução foi dividida em três aplicações executáveis:

1. **InventoryService**: proprietário dos produtos, saldos e operações de reserva/liberação.
2. **BillingService**: proprietário das notas, itens, numeração e fechamento.
3. **Angular UI**: aplicação web para o uso das funcionalidades e demonstração das falhas.

O Nginx entrega a interface e encaminha `/api/products` e `/api/stock` ao Estoque, e `/api/invoices` ao Faturamento. Cada serviço utiliza um banco PostgreSQL separado.

## 2. Angular

### Ciclos de vida utilizados

- `ngOnInit`: carrega os produtos e as notas quando cada página é iniciada.
- Não foi necessário utilizar `ngOnDestroy`, porque as chamadas HTTP do `HttpClient` completam após uma resposta. Não existem assinaturas de longa duração mantidas pelos componentes.

### RxJS

- `forkJoin`: carrega produtos e notas em paralelo no painel e na tela de faturamento.
- `finalize`: encerra os indicadores de carregamento tanto em sucesso quanto em erro.
- `catchError` e `throwError`: usados no interceptor HTTP para apresentar mensagens consistentes e preservar o erro para o componente chamador.
- `Observable`: contrato de retorno do serviço que concentra as chamadas HTTP.

### Componentes e gerenciamento de estado

- Foram utilizados componentes standalone e rotas lazy-loaded.
- `signal` armazena estado local reativo: listas, carregamento, formulário aberto, impressão atual e modo de falha.
- Reactive Forms valida os campos obrigatórios, saldos e quantidades.
- A aplicação não necessita de uma store global porque o estado é pequeno e pertence às páginas.

### Bibliotecas visuais

Não foi utilizada uma biblioteca de componentes pronta. Os elementos foram construídos com HTML semântico e CSS responsivo para demonstrar domínio da base do front-end e manter o bundle leve. A tipografia usa DM Sans e Manrope, com fallbacks do sistema.

## 3. Backend C#/.NET

### Frameworks e bibliotecas

- ASP.NET Core 8: APIs REST, injeção de dependência, health checks e middleware.
- Entity Framework Core: persistência, mapeamento e transações.
- Npgsql: provider PostgreSQL.
- Swashbuckle: documentação Swagger/OpenAPI.
- xUnit: testes automatizados.

As dependências são gerenciadas pelo NuGet em cada arquivo `.csproj` e restauradas durante o build do Docker ou da integração contínua.

### Uso de LINQ

LINQ é utilizado para:

- Ordenação e projeção das respostas.
- Agrupamento de itens repetidos de uma nota.
- Soma das quantidades por produto.
- Transformação das entidades em DTOs.
- Consultas assíncronas com `AnyAsync`, `SingleOrDefaultAsync` e `ToListAsync`.

## 4. Consistência e concorrência

O Estoque inicia uma transação e bloqueia cada produto com `SELECT ... FOR UPDATE`. Os produtos são bloqueados em uma ordem determinística por identificador, reduzindo o risco de deadlock. Antes da alteração, o serviço verifica se existe saldo suficiente.

Exemplo: se duas notas tentarem consumir simultaneamente um produto com saldo 1, uma transação obterá o bloqueio primeiro. Após seu commit, a segunda verá o saldo atualizado e receberá HTTP 409, sem permitir estoque negativo.

O Faturamento também bloqueia a nota durante a impressão. Assim, duas solicitações concorrentes não conseguem fechar a mesma nota simultaneamente.

## 5. Idempotência

O Faturamento envia ao Estoque a chave estável `invoice-{id}`. O Estoque mantém uma tabela de operações com índice único para essa chave e tipo de operação.

Se ocorrer uma queda depois da reserva do saldo e antes do commit do Faturamento, a próxima tentativa envia a mesma chave. O Estoque reconhece que já realizou a reserva, não desconta novamente e permite que o Faturamento finalize a nota.

Também existe uma chave enviada pelo cliente no cabeçalho `Idempotency-Key`, registrada na operação de impressão para rastreabilidade.

## 6. Tratamento de falhas e exceções

- O cliente HTTP do Faturamento realiza até três tentativas curtas para erros transitórios.
- Falhas de indisponibilidade retornam HTTP 503 com mensagem informando que a nota permaneceu Aberta.
- Saldo insuficiente retorna HTTP 409.
- Recursos inexistentes retornam HTTP 404.
- Exceções inesperadas retornam uma mensagem genérica, enquanto os detalhes são registrados nos logs.
- O botão permanece com indicador de processamento durante a chamada.
- Um interceptor centraliza as mensagens de erro no Angular.

O modo de demonstração envia `simulateInventoryFailure=true`. O Estoque responde 503 antes de iniciar qualquer alteração, tornando o cenário seguro e repetível.

## 7. Banco de dados

O PostgreSQL utiliza dois bancos físicos:

- `korp_inventory`
- `korp_billing`

As tabelas são criadas automaticamente na primeira execução. Restrições de unicidade protegem códigos, números e chaves idempotentes; checks impedem saldos negativos e quantidades inválidas.

## 8. Testes e automação

- Testes xUnit cobrem o cadastro válido e a rejeição de código duplicado.
- O script `scripts/smoke-test.sh` exercita o fluxo integrado: produto, nota e impressão.
- O GitHub Actions restaura, compila e testa o backend, além de instalar e gerar o build de produção do Angular.

## 9. Decisões e melhorias futuras

Para uma evolução em produção, seriam adicionados autenticação, autorização por função, migrations versionadas, observabilidade distribuída, mensageria com outbox, circuit breaker, paginação, logs centralizados e testes de integração com containers efêmeros.

