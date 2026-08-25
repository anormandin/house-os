import { expect, test } from '@playwright/test'

// Comptes seedés du dev (appsettings.json) ; surchargeables pour une autre pile.
const UTILISATEUR = process.env.E2E_UTILISATEUR ?? 'alain'
const MOT_DE_PASSE = process.env.E2E_MOT_DE_PASSE ?? '45234523'

function dansTroisJours(): string {
  const date = new Date()
  date.setDate(date.getDate() + 3)
  const mois = String(date.getMonth() + 1).padStart(2, '0')
  const jour = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${mois}-${jour}`
}

test('login → créer → éditer sans perte → compléter → annuler → supprimer', async ({ page }) => {
  const titre = `E2E fumée ${Date.now()}`
  const echeance = dansTroisJours()

  // Login
  await page.goto('/')
  await page.getByPlaceholder('alain ou ariane').fill(UTILISATEUR)
  await page.getByLabel('Mot de passe').fill(MOT_DE_PASSE)
  await page.getByRole('button', { name: 'Entrer' }).click()
  await page.getByRole('link', { name: 'Tâches' }).click()
  await expect(page.getByRole('heading', { name: 'Toutes les tâches' })).toBeVisible()

  // Créer via le quick-add : le modal complet doit s'ouvrir
  await page.getByRole('button', { name: /Ajouter une tâche/ }).click()
  await expect(page.getByRole('heading', { name: 'Nouvelle tâche' })).toBeVisible()
  await page.getByLabel('Titre').fill(titre)
  await page.getByLabel('Description').fill('Ligne 1\nLigne 2')
  await page.getByLabel('Échéance', { exact: true }).fill(echeance)
  await page.getByRole('button', { name: 'Enregistrer' }).click()
  const rangee = page.getByRole('listitem').filter({ hasText: titre })
  await expect(rangee).toBeVisible()

  // Éditer sans rien changer : l'échéance doit survivre (bug du 2026-08-25)
  await rangee.getByText(titre).click()
  await expect(page.getByLabel('Échéance', { exact: true })).toHaveValue(echeance)
  await page.getByRole('button', { name: 'Enregistrer' }).click()
  await rangee.getByText(titre).click()
  await expect(page.getByLabel('Échéance', { exact: true })).toHaveValue(echeance)
  await page.getByRole('button', { name: 'Annuler', exact: true }).click()

  // Compléter, puis annuler la complétion
  await rangee.getByRole('button', { name: `Compléter ${titre}` }).click()
  await expect(rangee).toBeHidden() // le filtre À faire ne la montre plus
  await page.getByRole('button', { name: 'Complétées' }).click()
  const faite = page.getByRole('listitem').filter({ hasText: titre })
  await expect(faite.getByText(/bravo/)).toBeVisible()
  await faite.hover()
  await faite.getByRole('button', { name: `Annuler la complétion de ${titre}` }).click()
  await expect(faite).toBeHidden()

  // Supprimer (nettoyage) : poubelle + confirmation « Vraiment ? »
  await page.getByRole('button', { name: 'À faire' }).click()
  await expect(rangee).toBeVisible()
  await rangee.hover()
  await rangee.getByRole('button', { name: `Supprimer la tâche ${titre}` }).click()
  // Le même bouton devient la pastille de confirmation « Vraiment? »
  await rangee.getByRole('button', { name: `Supprimer la tâche ${titre}` }).click()
  await expect(rangee).toBeHidden()
})
