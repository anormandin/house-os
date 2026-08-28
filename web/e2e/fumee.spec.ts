import { expect, test } from '@playwright/test'
import {
  caseCompleter,
  confirmerSuppression,
  dansNJours,
  ouvrirConsoleTaches,
  rangeeConsole,
  seConnecter,
} from './aide'

// Parcours fumée sur l'UI actuelle (console Liste + Aujourd'hui) — réécrit lors
// de la ronde QA 2026-08-28, l'ancien spec visait les filtres « À faire /
// Complétées » retirés à la refonte de la console.
test('login → créer → éditer sans perte → compléter → annuler → supprimer', async ({ page }) => {
  const titre = `E2E fumée ${Date.now()}`
  const echeance = dansNJours(3)

  await seConnecter(page)
  await ouvrirConsoleTaches(page)

  // Créer via « Nouvelle » : le modal complet s'ouvre
  await page.getByRole('button', { name: 'Nouvelle' }).click()
  await expect(page.getByRole('heading', { name: 'Nouvelle tâche' })).toBeVisible()
  await page.getByLabel('Titre').fill(titre)
  await page.getByLabel('Description').fill('Ligne 1\nLigne 2')
  await page.getByLabel('Échéance', { exact: true }).fill(echeance)
  await page.getByRole('button', { name: 'Enregistrer' }).click()
  await expect(rangeeConsole(page, titre)).toBeVisible()

  // Éditer sans rien changer : l'échéance doit survivre (bug du 2026-08-25)
  await rangeeConsole(page, titre).click()
  await expect(page.getByLabel('Échéance', { exact: true })).toHaveValue(echeance)
  await page.getByRole('button', { name: 'Enregistrer' }).click()
  await rangeeConsole(page, titre).click()
  await expect(page.getByLabel('Échéance', { exact: true })).toHaveValue(echeance)
  await page.getByRole('button', { name: 'Annuler', exact: true }).click()

  // Compléter depuis la console (case à cocher de la rangée)
  await caseCompleter(page, titre).click()
  await expect(caseCompleter(page, titre)).toBeHidden()

  // La complétion du jour apparaît sur Aujourd'hui ; on l'annule
  await page.getByRole('link', { name: /^Aujourd/ }).click()
  const faite = page.getByRole('listitem').filter({ hasText: titre })
  await expect(faite).toBeVisible()
  await faite.hover()
  await faite.getByRole('button', { name: `Annuler la complétion de ${titre}` }).click()
  await expect(faite).toBeHidden()

  // Retour console : la case est revenue, puis suppression (deux temps)
  await ouvrirConsoleTaches(page)
  await expect(caseCompleter(page, titre)).toBeVisible()
  await rangeeConsole(page, titre).click()
  await confirmerSuppression(page, 'Supprimer la tâche')
  await expect(rangeeConsole(page, titre)).toBeHidden()
})
