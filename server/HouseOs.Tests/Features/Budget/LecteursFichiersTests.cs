using HouseOs.Api.Features.Budget;

namespace HouseOs.Tests.Features.Budget;

public class LecteursFichiersTests
{
    private const string Ofx = """
        OFXHEADER:100
        DATA:OFXSGML
        VERSION:102
        <OFX>
        <BANKMSGSRSV1><STMTTRNRS><STMTRS>
        <BANKTRANLIST>
        <STMTTRN>
        <TRNTYPE>DEBIT
        <DTPOSTED>20260824120000[-5:EST]
        <TRNAMT>-84.12
        <FITID>2026082401
        <NAME>CANADIAN TIRE #213
        </STMTTRN>
        <STMTTRN>
        <TRNTYPE>CREDIT
        <DTPOSTED>20260826
        <TRNAMT>675.00
        <FITID>2026082601
        <MEMO>VIR INTERAC DEPOT
        </STMTTRN>
        </BANKTRANLIST>
        </STMTRS></STMTTRNRS></BANKMSGSRSV1>
        </OFX>
        """;

    [Fact]
    public void Ofx_lit_dates_montants_fitid_et_descriptions()
    {
        var transactions = LecteurOfx.Lire(Ofx);

        Assert.Equal(2, transactions.Count);
        Assert.Equal(new DateOnly(2026, 8, 24), transactions[0].Date);
        Assert.Equal(-84.12m, transactions[0].Montant);
        Assert.Equal("2026082401", transactions[0].IdExterne);
        Assert.Equal("CANADIAN TIRE #213", transactions[0].Description);
        // NAME absent → MEMO en repli
        Assert.Equal("VIR INTERAC DEPOT", transactions[1].Description);
        Assert.Equal(675.00m, transactions[1].Montant);
    }

    [Fact]
    public void Ofx_sans_transaction_est_refuse()
    {
        Assert.Throws<FormatFichierException>(() => LecteurOfx.Lire("<OFX></OFX>"));
    }

    [Fact]
    public void Csv_desjardins_retrait_et_depot_en_colonnes_separees()
    {
        // Forme AccWeb : compte;date;numéro;description;retrait;dépôt;solde
        const string csv = """
            "EOP";"2026-08-24";"213";"CANADIAN TIRE #213";"84,12";"";"12 395,88"
            "EOP";"2026-08-26";"214";"VIR INTERAC DEPOT";"";"675,00";"13 070,88"
            """;
        var transactions = LecteurCsv.Lire(csv);

        Assert.Equal(2, transactions.Count);
        Assert.Equal(-84.12m, transactions[0].Montant);
        Assert.Equal("CANADIAN TIRE #213", transactions[0].Description);
        Assert.Null(transactions[0].IdExterne);
        // Le numéro de séquence AccWeb est capturé — il distingue deux transactions
        // identiques le même jour dans la clé de dédup.
        Assert.Equal("213", transactions[0].NumeroSequence);
        Assert.Equal("214", transactions[1].NumeroSequence);
        Assert.Equal(675.00m, transactions[1].Montant);
        Assert.Equal(new DateOnly(2026, 8, 26), transactions[1].Date);
    }

    [Fact]
    public void Csv_desjardins_retrait_et_depot_sans_colonne_solde()
    {
        // Deux colonnes dont une vide sur chaque ligne : retrait/dépôt, pas montant/solde.
        const string csv = """
            "2026-08-24";"CANADIAN TIRE #213";"84,12";""
            "2026-08-26";"VIR INTERAC DEPOT";"";"675,00"
            """;
        var transactions = LecteurCsv.Lire(csv);

        Assert.Equal(2, transactions.Count);
        Assert.Equal(-84.12m, transactions[0].Montant);
        Assert.Equal(675.00m, transactions[1].Montant);
    }

    [Fact]
    public void Csv_montant_et_solde_lit_le_montant_signe_et_ignore_le_solde()
    {
        // Format courant hors AccWeb : la 2ᵉ colonne est un solde cumulatif, pas un dépôt.
        const string csv = """
            Date,Description,Montant,Solde
            2026-08-24,ACHAT PEINTURE,-84.12,12395.88
            2026-08-25,ACHAT VIS,-15.00,12380.88
            2026-08-26,VIR INTERAC DEPOT,675.00,13055.88
            """;
        var transactions = LecteurCsv.Lire(csv);

        Assert.Equal(3, transactions.Count);
        Assert.Equal(-84.12m, transactions[0].Montant);
        Assert.Equal(-15.00m, transactions[1].Montant);
        Assert.Equal(675.00m, transactions[2].Montant);
    }

    [Fact]
    public void Csv_montant_et_solde_en_ordre_antichronologique_aussi()
    {
        // Les banques exportent souvent du plus récent au plus ancien.
        const string csv = """
            Date,Description,Montant,Solde
            2026-08-26,VIR INTERAC DEPOT,675.00,13055.88
            2026-08-25,ACHAT VIS,-15.00,12380.88
            2026-08-24,ACHAT PEINTURE,-84.12,12395.88
            """;
        var transactions = LecteurCsv.Lire(csv);

        Assert.Equal(3, transactions.Count);
        Assert.Equal(675.00m, transactions[0].Montant);
        Assert.Equal(-84.12m, transactions[2].Montant);
    }

    [Fact]
    public void Csv_a_deux_colonnes_ambigues_est_refuse_plutot_que_devine()
    {
        // Deux colonnes toujours remplies, mais la 2ᵉ n'est pas un cumul de la 1ʳᵉ :
        // impossible de trancher entre « retrait, dépôt » et « montant, solde ».
        const string csv = """
            2026-08-24,MYSTERE UN,10.00,20.00
            2026-08-25,MYSTERE DEUX,30.00,90.00
            """;
        var erreur = Assert.Throws<FormatFichierException>(() => LecteurCsv.Lire(csv));

        Assert.Contains("ambigu", erreur.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Csv_a_montant_signe_unique_accepte()
    {
        const string csv = """
            Date,Description,Montant
            2026-08-24,CANADIAN TIRE #213,-84.12
            2026-08-26,VIR INTERAC DEPOT,675.00
            """;
        var transactions = LecteurCsv.Lire(csv);

        Assert.Equal(2, transactions.Count);
        Assert.Equal(-84.12m, transactions[0].Montant);
        Assert.Equal(675.00m, transactions[1].Montant);
    }

    [Fact]
    public void Csv_sans_ligne_reconnaissable_est_refuse()
    {
        Assert.Throws<FormatFichierException>(() => LecteurCsv.Lire("bonjour\nrien à voir"));
    }

    [Fact]
    public void Fournisseur_route_ofx_et_csv()
    {
        var fournisseur = new FournisseurFichier();
        Assert.Equal(2, fournisseur.Lire(Ofx, "releve.ofx").Count);
        Assert.Single(fournisseur.Lire("\"2026-08-24\";\"ACHAT\";\"-10,00\"", "releve.csv"));
    }
}
