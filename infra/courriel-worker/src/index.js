// Reçoit un courriel routé par Cloudflare Email Routing et le dépose tel quel (MIME
// brut) dans R2. Aucun parsing ici : House OS relève le bucket et fait le reste
// (server/HouseOs.Api/Features/Courriel). Voir la décision
// « D-2026-09-02 Courriel Entrant Par Cloudflare Et R2 » dans le vault.

const TAILLE_MAX = 25 * 1024 * 1024; // limite Email Routing, gardée ici par clarté

export default {
  async email(message, env) {
    const permis = (env.EXPEDITEURS_PERMIS ?? "")
      .split(",")
      .map((adresse) => adresse.trim().toLowerCase())
      .filter((adresse) => adresse.length > 0);
    const expediteur = message.from.toLowerCase();
    if (permis.includes(expediteur) === false) {
      // 5xx au SMTP : l'expéditeur inconnu reçoit un rebond, rien n'entre.
      message.setReject("Expéditeur non autorisé");
      return;
    }
    if (message.rawSize > TAILLE_MAX) {
      message.setReject("Message trop volumineux (25 Mo max)");
      return;
    }

    // R2 exige une longueur connue : message.raw est un flux sans longueur, on le
    // lit d'abord en mémoire (≤ 25 Mo, la limite d'Email Routing).
    const corps = await new Response(message.raw).arrayBuffer();
    const horodatage = new Date().toISOString().replace(/[:.]/g, "-");
    const cle = `${env.PREFIXE ?? "entrants/"}${horodatage}-${crypto.randomUUID()}.eml`;
    await env.COURRIELS.put(cle, corps, {
      httpMetadata: { contentType: "message/rfc822" },
      customMetadata: {
        de: expediteur,
        a: message.to,
        sujet: (message.headers.get("subject") ?? "").slice(0, 500),
        messageId: (message.headers.get("message-id") ?? "").slice(0, 300),
      },
    });
  },
};
