import { expect, test } from '@playwright/test'
import { dansNJours, seConnecter } from './aide'

// Parcours budget (QA 2026-08-28) : ancrage (si première visite) → enveloppe →
// ajustement → import CSV AccWeb → rapprochement du retrait → solde à zéro →
// fermeture de l'enveloppe (le nettoyage : une enveloppe ne se supprime jamais).
test('ancrage → enveloppe → import → rapprochement → fermeture', async ({ page }) => {
  const horodatage = Date.now()
  const nomEnveloppe = `E2E Projet ${horodatage}`
  const descriptionTransaction = `E2E ACHAT ${horodatage}`
  const aujourdhui = dansNJours(0)

  await seConnecter(page)
  await page.getByRole('link', { name: 'Budget' }).click()

  // Premier écran : ancrer le compte si la base est vierge. L'attente du h1
  // (« Budget » ou « Ancrer le fonds de prévoyance ») évite de trancher pendant
  // le « Chargement… ».
  const h1 = page.getByRole('heading', { level: 1 })
  await expect(h1).toBeVisible()
  if ((await h1.textContent())?.includes('Ancrer')) {
    await page.getByLabel('Nom du compte').fill('Fonds E2E')
    await page.getByLabel('Solde initial').fill('1000')
    await page.getByLabel("Date d'ancrage").fill(dansNJours(-30))
    await page.getByRole('button', { name: /Ancrer/ }).click()
  }
  await expect(page.getByRole('button', { name: 'Nouvelle enveloppe' })).toBeVisible()

  // Créer l'enveloppe (type par défaut, cible libre)
  await page.getByRole('button', { name: 'Nouvelle enveloppe' }).click()
  await page.getByLabel('Nom', { exact: true }).fill(nomEnveloppe)
  await page.getByLabel('Montant cible').fill('500')
  await page.getByRole('button', { name: 'Enregistrer' }).click()
  const carte = page.getByText(nomEnveloppe).first()
  await expect(carte).toBeVisible()

  // Provisionner 42,42 $ par ajustement rapide (rouvre la fiche)
  await carte.click()
  await page.getByLabel("Montant d'ajustement").fill('42.42')
  await page.getByRole('button', { name: 'Ajouter', exact: true }).click()
  await expect(page.getByText('Ajustement', { exact: true }).first()).toBeVisible()
  await page.getByRole('button', { name: 'Annuler', exact: true }).click()

  // Importer un CSV AccWeb contenant un retrait du même montant
  const csv = `"EOP";"${aujourdhui}";"991";"${descriptionTransaction}";"42,42";"";"9 999,99"\n`
  await page.locator('input[type="file"]').setInputFiles({
    name: `e2e-${horodatage}.csv`,
    mimeType: 'text/csv',
    buffer: Buffer.from(csv, 'utf-8'),
  })
  await expect(page.getByText(descriptionTransaction)).toBeVisible()

  // Lier le retrait à l'enveloppe : le solde revient à zéro
  await page
    .locator('div, li')
    .filter({ hasText: descriptionTransaction })
    .getByRole('button', { name: 'Lier', exact: true })
    .first()
    .click()
  await expect(page.getByRole('heading', { name: 'Lier le retrait' })).toBeVisible()
  await page.getByLabel('Enveloppe du retrait').selectOption({ label: nomEnveloppe })
  await page.getByRole('button', { name: 'Lier', exact: true }).last().click()
  await expect(page.getByRole('heading', { name: 'Lier le retrait' })).toBeHidden()
  await expect(page.getByText(descriptionTransaction, { exact: true })).toBeHidden()

  // Fermer l'enveloppe : le bouton n'est actif qu'à solde zéro — sa réussite
  // (modal fermé) PROUVE que le rapprochement a bien débité 42,42 $.
  await page.getByRole('button', { name: new RegExp(nomEnveloppe) }).click()
  const fermer = page.getByRole('button', { name: 'Fermer l’enveloppe' })
  await expect(fermer).toBeEnabled()
  await fermer.click()
  await expect(page.getByRole('heading', { name: nomEnveloppe })).toBeHidden()
})
