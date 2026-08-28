import { expect, test } from '@playwright/test'
import {
  caseCompleter,
  confirmerSuppression,
  ouvrirConsoleTaches,
  rangeeConsole,
  seConnecter,
} from './aide'

// Parcours récurrence (QA 2026-08-28) : créer une récurrente, la compléter
// depuis la console, vérifier que la prochaine occurrence se matérialise.
test('créer récurrente → compléter → la prochaine occurrence se matérialise', async ({ page }) => {
  const titre = `E2E récurrence ${Date.now()}`

  await seConnecter(page)
  await ouvrirConsoleTaches(page)

  // Créer une tâche « après la dernière fois » aux 7 jours
  await page.getByRole('button', { name: 'Nouvelle' }).click()
  await expect(page.getByRole('heading', { name: 'Nouvelle tâche' })).toBeVisible()
  await page.getByLabel('Titre').fill(titre)
  await page.getByRole('button', { name: 'Après la dernière fois' }).click()
  await page.getByRole('button', { name: 'Enregistrer' }).click()

  await expect(rangeeConsole(page, titre)).toBeVisible()
  await expect(caseCompleter(page, titre)).toBeVisible()

  // Compléter : la prochaine occurrence doit se matérialiser (la case revient
  // après l'invalidation croisée) — c'est le cœur du moteur.
  await caseCompleter(page, titre).click()
  await expect(caseCompleter(page, titre)).toBeVisible({ timeout: 10_000 })

  // La complétion est au journal d'Aujourd'hui pendant que la suivante attend.
  await page.getByRole('link', { name: /^Aujourd/ }).click()
  await expect(page.getByRole('listitem').filter({ hasText: titre })).toBeVisible()

  // Nettoyage : supprimer la définition (le journal, lui, survit — c'est voulu).
  await ouvrirConsoleTaches(page)
  await rangeeConsole(page, titre).click()
  await confirmerSuppression(page, 'Supprimer la tâche')
  await expect(rangeeConsole(page, titre)).toBeHidden()
})
