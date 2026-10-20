using System.Text;

namespace MaterDomus.VitrineSeoGen;

/// <summary>
/// Páginas institucionais servidas como HTML, para a primeira pintura não
/// esperar o runtime Blazor. O miolo troca o marcador de index.html.
/// </summary>
public static class StaticPages
{
    private sealed record Page(string FileName, string? NavKey, string Title, string Description, string Canonical, string Main, bool NoIndex = false);

    public static IReadOnlyDictionary<string, string> Render(string indexHtml)
    {
        var pages = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var page in Pages)
        {
            var html = VitrineSeo.ApplyDocumentMeta(indexHtml, page.Title, page.Description, page.Canonical);
            html = VitrineSeo.ReplacePageMain(html, page.Main);
            html = VitrineSeo.ActivateNav(html, page.NavKey);
            if (page.NoIndex)
                html = VitrineSeo.ReplaceMetaContent(html, "name", "robots", "noindex, nofollow");
            pages[page.FileName] = html;
        }

        return pages;
    }

    public static void WriteAll(string repoRoot, string indexHtml, Encoding encoding)
    {
        foreach (var (fileName, html) in Render(indexHtml))
        {
            File.WriteAllText(Path.Combine(repoRoot, "wwwroot", fileName), html, encoding);
        }
    }

    private static readonly Page[] Pages =
    [
        new(
            "sobre.html",
            "sobre",
            "Sobre a Mater Domus | Revendedora de Ou e Linha Flow",
            "A MATER DOMUS LTDA (CNPJ 64.258.511/0001-90, São Paulo/SP) revende utilidades Ou e Linha Flow, principalmente pela Amazon Brasil.",
            "https://www.materdomus.com.br/sobre",
            """
            <section><h1>Quem é a Mater Domus</h1>

                <p>
                    A <strong>Mater Domus</strong> é uma marca brasileira voltada para o e-commerce
                    de produtos para casa. Nosso foco está em curadoria, eficiência logística
                    e parcerias sólidas.
                </p>

                <p>
                    Acreditamos que bons negócios são construídos com transparência,
                    comunicação clara e visão de longo prazo.
                </p>

                <p>
                    A marca <strong>Mater Domus</strong> é operada pela
                    <strong>MATER DOMUS LTDA</strong>, CNPJ 64.258.511/0001-90, em São Paulo/SP.
                    Este site é www.materdomus.com.br. Não é a empresa do domínio materdomus.com
                    e não é uma loja de informática.
                </p>

                <p>
                    A empresa revende utilidades domésticas das marcas <strong>Ou</strong> e
                    <strong>Linha Flow</strong>. Essas marcas são do fabricante Martiplast,
                    de Caxias do Sul/RS, que também vende direto em
                    <a href="https://ou.com.br/" target="_blank" rel="noopener noreferrer">ou.com.br</a>.
                    A Mater Domus não fabrica esses produtos.
                </p>

                <p>
                    A venda é feita principalmente pela Amazon Brasil, com envio pela Amazon (FBA).
                    Na vitrine, o botão Comprar na Amazon abre o anúncio quando o item está disponível.
                    Pagamento, frete, troca e devolução dessa compra são tratados pela Amazon Brasil.
                </p>
            </section>
            <section><h2>Nossos valores</h2>

                <ul><li>✔ Compromisso com parceiros</li>
                    <li>✔ Responsabilidade comercial</li>
                    <li>✔ Crescimento sustentável</li>
                    <li>✔ Foco na experiência do cliente final</li></ul>
            </section>
            """),
        new(
            "fornecedores.html",
            "fornecedores",
            "Fornecedores e parcerias | Mater Domus",
            "Seja fornecedor da Mater Domus. Buscamos fabricantes e distribuidores de utilidades domésticas e organização.",
            "https://www.materdomus.com.br/fornecedores",
            """
            <section class="hero"><h2>Parcerias que crescem junto</h2>
                <p>
                    Estamos em busca de fornecedores confiáveis para construir
                    uma operação sólida e escalável no e-commerce.
                </p></section>

            <section><h3>Quem buscamos</h3>
                <p>
                    Fabricantes, distribuidores ou importadores de produtos para casa,
                    organização e utilidades domésticas que compartilhem valores de
                    qualidade, compromisso e profissionalismo.
                </p></section>

            <section><h3>Por que ser fornecedor da Mater Domus?</h3>

                <ul><li>🤝 Relacionamento transparente</li>
                    <li>📦 Potencial de compras recorrentes</li>
                    <li>📈 Crescimento conjunto</li>
                    <li>🛒 Experiência com marketplaces</li>
                    <li>📊 Análise e validação de produtos</li></ul></section>

            <section><h3>Como funciona</h3>

                <ol><li>Contato e apresentação do portfólio</li>
                    <li>Análise de viabilidade comercial</li>
                    <li>Pedido piloto</li>
                    <li>Escala de compras</li></ol></section>

            <section class="cta"><h3>Quer ser nosso fornecedor?</h3>
                <p>
                    Entre em contato e apresente seus produtos.
                    Será um prazer conversar.
                </p>

                <a class="btn-primary" href="/contato" data-track='{"event":"supplier_intent","page":"fornecedores","intent_level":"high","next_step":"contato"}'>Entrar em contato</a></section>
            """),
        new(
            "contato.html",
            "contato",
            "Contato | Mater Domus",
            "Fale com a Mater Domus por e-mail, WhatsApp ou formulário. Atendimento a clientes, fornecedores e parceiros.",
            "https://www.materdomus.com.br/contato",
            """
            <section>
                <h1>Fale com a Mater Domus</h1>

                <p>
                    Estamos disponíveis para conversar com fornecedores,
                    parceiros logísticos e oportunidades comerciais.
                </p>

                <p>
                    Prefira o formulário para propostas e parcerias.
                    WhatsApp e e-mail continuam disponíveis.
                </p>

                <p>
                    Pagamento, frete, troca e devolução de compras feitas pelo link da
                    Amazon são tratados pela Amazon Brasil. Veja
                    <a href="/frete-e-trocas">Frete e trocas</a>.
                </p>
            </section>

            <section class="forms-section" data-track-focus='{"event":"supplier_contact_click","page":"contato","channel":"microsoft_forms","lead_type":"supplier","action":"section_focus"}'>
                <h2>Formulário para fornecedores e parceiros</h2>
                <p>
                    Envie sua proposta pelo formulário. É o canal preferencial
                    para apresentações comerciais.
                </p>

                <a class="btn-primary forms-open-btn"
                   href="https://forms.cloud.microsoft/r/jyc4aTkNQj"
                   target="_blank"
                   rel="noopener noreferrer"
                   data-track='{"event":"supplier_contact_click","page":"contato","channel":"microsoft_forms","lead_type":"supplier","action":"open_button"}'>Abrir formulário</a>

                <div class="forms-embed-wrapper">
                    <iframe class="forms-embed"
                            src="https://forms.cloud.microsoft/r/jyc4aTkNQj?embed=true"
                            title="Formulário de contato para fornecedores e parceiros"
                            loading="lazy"
                            allowfullscreen>
                    </iframe>
                </div>
            </section>

            <section>
                <h2>Outros canais</h2>
                <p>
                    📧 <strong>Email:</strong> <a href="mailto:contato@materdomus.com.br" data-track='{"event":"supplier_contact_click","page":"contato","channel":"email","lead_type":"supplier"}'>contato@materdomus.com.br</a><br>
                    📱 <a href="https://wa.me/5519993491775" target="_blank" rel="noopener noreferrer" data-track='{"event":"supplier_contact_click","page":"contato","channel":"whatsapp","lead_type":"supplier"}'><strong>WhatsApp</strong></a>
                </p>

                <p>
                    Atendimento em horário comercial. O horário detalhado será informado em breve.
                </p>
            </section>
            """),
        new(
            "privacidade.html",
            null,
            "Política de Privacidade | Mater Domus",
            "Como a Mater Domus trata dados pessoais, cookies e analytics no site institucional, em conformidade com a LGPD. Contato: contato@materdomus.com.br.",
            "https://www.materdomus.com.br/privacidade",
            """
            <section class="privacy-page">
                <h1>Política de Privacidade</h1>

                <p>
                    A <strong>Mater Domus</strong> (razão social <strong>MATER DOMUS LTDA</strong>)
                    respeita a privacidade de quem visita este site e de quem fala conosco.
                    Esta página explica, em linguagem simples, quais dados podem ser tratados,
                    para quê, com quem são compartilhados e quais são os seus direitos
                    pela Lei Geral de Proteção de Dados (LGPD — Lei nº 13.709/2018).
                </p>

                <h2>1. Quem é o controlador</h2>
                <p>
                    O controlador dos dados tratados neste site é a
                    <strong>MATER DOMUS LTDA</strong>, que opera a marca <strong>Mater Domus</strong>.
                </p>
                <ul>
                    <li>Site: <a href="https://www.materdomus.com.br">https://www.materdomus.com.br</a></li>
                    <li>E-mail: <a href="mailto:contato@materdomus.com.br">contato@materdomus.com.br</a></li>
                    <li>WhatsApp: <a href="https://wa.me/5519993491775" target="_blank" rel="noopener noreferrer">+55 19 99349-1775</a></li>
                </ul>

                <h2>2. Quais dados podemos tratar</h2>
                <p>Dependendo de como você usa o site, podemos tratar:</p>
                <ul>
                    <li>
                        <strong>Navegação e analytics</strong> — páginas visitadas, dispositivo aproximado,
                        origem do acesso e métricas de uso, apenas se você <strong>aceitar</strong>
                        cookies de analytics (via Google Tag Manager).
                    </li>
                    <li>
                        <strong>Contato</strong> — nome, e-mail, telefone e o conteúdo da mensagem
                        quando você nos escreve por e-mail, WhatsApp ou pelo formulário Microsoft Forms
                        na página <a href="/contato">Contato</a>.
                    </li>
                    <li>
                        <strong>Compras</strong> — a vitrine de produtos neste site aponta para a Amazon.
                        A compra é concluída na Amazon. Dados de pagamento, entrega e conta de cliente
                        são tratados pela Amazon, que é responsável por aquele checkout.
                        Frete, troca e devolução dessa compra também ficam com a Amazon Brasil:
                        <a href="/frete-e-trocas">Frete e trocas</a>.
                    </li>
                </ul>
                <p>
                    Não pedimos documentos sensíveis neste site. Evite enviar dados desnecessários
                    nas mensagens de contato.
                </p>

                <h2>3. Para que usamos os dados e bases legais</h2>
                <ul>
                    <li>
                        <strong>Cookies de analytics (Google Tag Manager / Google Analytics, se ativo)</strong>
                        — finalidade: entender audiência e melhorar o site.
                        Base: <strong>consentimento</strong> (art. 7º, I, da LGPD). Sem o aceite,
                        essas tags não são carregadas.
                    </li>
                    <li>
                        <strong>Atendimento e propostas</strong> — responder e-mails, WhatsApp e
                        formulários de clientes, fornecedores e parceiros.
                        Base: <strong>execução de procedimentos preliminares ou do contrato</strong>
                        a pedido do titular, e/ou legítimo interesse em atender quem nos procura,
                        quando aplicável.
                    </li>
                    <li>
                        <strong>Funcionamento básico do site</strong> — lembrar sua escolha de cookies
                        (armazenada no seu navegador) para não repetir o aviso a cada visita.
                    </li>
                </ul>

                <h2>4. Compartilhamento</h2>
                <p>Podemos compartilhar dados com:</p>
                <ul>
                    <li>
                        <strong>Google</strong> — se você aceitar cookies de analytics, o Google Tag Manager
                        (e ferramentas configuradas nele, como o Google Analytics) pode tratar dados
                        de navegação. Política do Google:
                        <a href="https://policies.google.com/privacy" target="_blank" rel="noopener noreferrer">policies.google.com/privacy</a>.
                    </li>
                    <li>
                        <strong>Microsoft</strong> — se você usar o formulário na página Contato
                        (Microsoft Forms), a Microsoft trata os dados enviados no formulário.
                        Informações:
                        <a href="https://privacy.microsoft.com/pt-br" target="_blank" rel="noopener noreferrer">privacy.microsoft.com</a>.
                    </li>
                    <li>
                        <strong>Amazon</strong> — para compras feitas a partir dos links de produtos.
                        A Amazon é responsável pelo tratamento no checkout e na conta Amazon.
                        Informações:
                        <a href="https://www.amazon.com.br/privacy" target="_blank" rel="noopener noreferrer">Aviso de Privacidade da Amazon</a>.
                    </li>
                    <li>
                        Prestadores de hospedagem e infraestrutura do site (por exemplo, Azure Static Web Apps),
                        na medida necessária para manter o site no ar.
                    </li>
                </ul>
                <p>
                    Não vendemos seus dados pessoais. Transferências internacionais podem ocorrer
                    quando usamos Google, Microsoft ou Amazon, segundo as regras desses provedores
                    e a LGPD.
                </p>

                <h2>5. Por quanto tempo guardamos</h2>
                <ul>
                    <li>
                        <strong>Mensagens de contato</strong> — pelo tempo necessário para atender
                        o pedido e eventuais obrigações legais ou de defesa, em geral até 12 meses
                        após o encerramento do atendimento, salvo prazo maior exigido por lei.
                    </li>
                    <li>
                        <strong>Analytics</strong> — conforme a retenção configurada nas ferramentas
                        do Google, enquanto o consentimento estiver válido.
                    </li>
                    <li>
                        <strong>Escolha de cookies</strong> — fica no seu aparelho (localStorage)
                        até você limpar os dados do site ou alterar a preferência.
                    </li>
                </ul>

                <h2>6. Seus direitos</h2>
                <p>
                    Você pode solicitar, na forma da LGPD: confirmação de tratamento, acesso,
                    correção de dados incompletos ou desatualizados, anonimização, bloqueio ou
                    eliminação de dados desnecessários, portabilidade (quando aplicável),
                    informação sobre compartilhamentos, revogação do consentimento e oposição
                    a tratamentos que não dependam de consentimento, quando cabível.
                </p>
                <p>
                    Para exercer esses direitos, escreva para
                    <a href="mailto:contato@materdomus.com.br">contato@materdomus.com.br</a>
                    com o assunto “LGPD”. Podemos pedir informações para confirmar a identidade
                    de quem solicita. Você também pode reclamar à Autoridade Nacional de
                    Proteção de Dados (ANPD).
                </p>

                <h2>7. Cookies e tecnologias semelhantes</h2>
                <p>
                    Este site pode usar cookies e tecnologias equivalentes. Distinguimos:
                </p>
                <ul>
                    <li>
                        <strong>Essenciais</strong> — o necessário para o site funcionar, inclusive
                        gravar se você aceitou ou recusou analytics. Sem eles, o aviso de cookies
                        voltaria a cada visita.
                    </li>
                    <li>
                        <strong>Analytics</strong> — só após o botão <strong>Aceitar</strong>.
                        São carregados pelo Google Tag Manager (identificador
                        <code>GTM-5FJ38X8C</code>) e podem incluir cookies do Google Analytics
                        para métricas de audiência e melhoria do site.
                    </li>
                </ul>
                <p>
                    Se você recusar, o site continua utilizável. Não carregamos o Google Tag Manager
                    nem tags de analytics. A vitrine, o contato e as demais páginas seguem disponíveis.
                </p>

                <h2>8. Como retirar o consentimento</h2>
                <p>Você pode mudar de ideia a qualquer momento:</p>
                <ul>
                    <li>
                        Abra <strong>Preferências de cookies</strong> no rodapé e escolha
                        <strong>Recusar</strong> (ou <strong>Aceitar</strong> de novo).
                    </li>
                    <li>
                        Ou limpe os dados deste site no navegador (armazenamento local / cookies)
                        e recarregue a página — o aviso volta a aparecer.
                    </li>
                </ul>
                <p>
                    Se o Google Tag Manager já tiver sido carregado nesta visita, ao recusar
                    recarregamos a página para interromper as tags de analytics.
                </p>

                <h2>9. Crianças e adolescentes</h2>
                <p>
                    O site não é dirigido a crianças. Não coletamos dados de menores de forma intencional.
                    Se você for responsável legal e identificar um tratamento indevido, fale conosco
                    para que possamos apagar as informações.
                </p>

                <h2>10. Atualizações desta política</h2>
                <p>
                    Podemos atualizar este texto para refletir mudanças no site ou na lei.
                    A data abaixo indica a versão publicada. Alterações relevantes serão
                    refletidas nesta página.
                </p>

                <p>
                    <strong>Última atualização:</strong> 24 de setembro de 2026.
                </p>

                <p>
                    Dúvidas: <a href="mailto:contato@materdomus.com.br">contato@materdomus.com.br</a>
                    ou <a href="/contato">página de Contato</a>.
                </p>
            </section>
            """),
        new(
            "frete-e-trocas.html",
            null,
            "Frete e trocas | Mater Domus",
            "Pagamento, frete, entrega, troca e devolução de compras feitas pelo link da Amazon são tratados pela Amazon Brasil. O site Mater Domus é uma vitrine.",
            "https://www.materdomus.com.br/frete-e-trocas",
            """
            <section class="privacy-page">
                <h1>Frete, trocas e devoluções</h1>

                <p>
                    O site da <strong>Mater Domus</strong> é uma <strong>vitrine</strong>.
                    Ele apresenta os produtos e, quando o item está disponível, o botão
                    <strong>Comprar na Amazon</strong> abre o anúncio na
                    <strong>Amazon Brasil</strong>.
                </p>

                <p>
                    Pagamento, frete, entrega, troca e devolução de uma compra feita por
                    esse link são tratados pela <strong>Amazon Brasil</strong>, nas condições
                    do anúncio e da conta de quem compra.
                </p>

                <h2>Frete e entrega</h2>
                <p>
                    Prazo e valor de frete aparecem na finalização do pedido na Amazon,
                    conforme o CEP, a disponibilidade e as opções daquele anúncio.
                    A Mater Domus não calcula frete neste site e não publica prazo próprio
                    de entrega.
                </p>

                <h2>Trocas e devoluções</h2>
                <p>
                    Troca, devolução e reembolso de um pedido feito na Amazon seguem as
                    regras da Amazon Brasil para aquela compra. A solicitação é feita na
                    conta Amazon de quem comprou, ou pela Central de Ajuda da Amazon:
                </p>
                <p>
                    <a href="https://www.amazon.com.br/gp/help/customer/display.html"
                       target="_blank"
                       rel="noopener noreferrer">amazon.com.br/gp/help/customer/display.html</a>
                </p>

                <h2>Contato com a Mater Domus</h2>
                <p>
                    Dúvidas sobre a vitrine, a marca ou parcerias podem ir para a página
                    <a href="/contato">Contato</a>. Esse canal não substitui o atendimento
                    de pedidos da Amazon.
                </p>
                <p>
                    O tratamento de dados deste site está na
                    <a href="/privacidade">Política de Privacidade</a>.
                </p>

                <p>
                    <strong>Última atualização:</strong> 24 de setembro de 2026.
                </p>
            </section>
            """),
        new(
            "404.html",
            null,
            "Página não encontrada | Mater Domus",
            "Este endereço não existe no site da Mater Domus (MATER DOMUS LTDA, São Paulo/SP). Veja a vitrine de Ou e Linha Flow ou volte ao início.",
            "https://www.materdomus.com.br/404.html",
            """
            <section>
                <h1>Página não encontrada</h1>
                <p>
                    O endereço pedido não existe neste site. A Mater Domus é a vitrine
                    de utilidades domésticas Ou e Linha Flow da MATER DOMUS LTDA, em São Paulo/SP.
                </p>
                <p>
                    <a href="/">Ir para o início</a>
                    ·
                    <a href="/produtos">Ver a vitrine</a>
                    ·
                    <a href="/contato">Falar com a Mater Domus</a>
                </p>
            </section>
            """,
            NoIndex: true)
    ];
}
