# 🚀 TESTER LA MISE À JOUR EN 3 ÉTAPES

## ✅ Tout est prêt pour le test !

J'ai compilé 3 versions de Panosse :
- **v1.1.0** → Celle que vous lancez
- **v1.1.1** → Celle que vous allez détecter
- (v1.0.0 → Bonus pour tester aussi)

---

## 🎯 3 ÉTAPES RAPIDES

### Étape 1 : Créer les releases (5 minutes)

#### Release v1.1.0
👉 **https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.1.0**

- Title : `Panosse v1.1.0`
- Uploadez les 2 fichiers de `release-v1.1.0\`
- Description : (voir `CREER-LES-2-RELEASES.md`)
- **Publiez** !

#### Release v1.1.1
👉 **https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.1.1**

- Title : `Panosse v1.1.1`
- Uploadez les 2 fichiers de `release-v1.1.1\`
- Description : (voir `TEST-MISE-A-JOUR.md`)
- **Publiez** !

⏱️ **Attendez 30 secondes** après la publication

---

### Étape 2 : Vérifier l'API (10 secondes)

```powershell
$r = Invoke-RestMethod -Uri "https://api.github.com/repos/barbarom84-ai/panosse/releases/latest" -Headers @{"User-Agent"="Test"}
Write-Host "Derniere version : $($r.tag_name)"
```

**Résultat attendu** : `v1.1.1` ✅

---

### Étape 3 : TESTER ! (30 secondes)

```powershell
cd release-v1.1.0
.\Panosse-v1.1.0.exe
```

**Résultat attendu** :
1. ✅ Fenêtre Panosse s'ouvre
2. ✅ Après 2-3 secondes : **Barre verte** apparaît en haut !
3. ✅ Message : "🔔 Une nouvelle version est disponible !"
4. ✅ Bouton "Mettre à jour"

---

## 🎉 Succès !

Si vous voyez la **barre verte**, c'est gagné ! 🎊

**Ensuite** :
1. Cliquez sur "Mettre à jour"
2. La barre de progression avance
3. Panosse se ferme et se relance automatiquement
4. Version passe à v1.1.1

**C'est magique ! ✨**

---

## 📖 Guides complets

Pour plus de détails :
- **`TEST-MISE-A-JOUR.md`** - Guide de test complet avec tous les scénarios
- **`CREER-LES-2-RELEASES.md`** - Instructions pour créer les releases

---

## 🔧 Si ça ne marche pas

### "Vérification impossible"
→ Les releases ne sont pas encore créées ou l'API n'est pas à jour  
→ Créez les releases et attendez 30 secondes

### "Version à jour"
→ Vous lancez déjà v1.1.1 au lieu de v1.1.0  
→ Lancez `release-v1.1.0\Panosse-v1.1.0.exe`

### Pas de barre verte
→ Vérifiez que l'API retourne bien v1.1.1 (commande ci-dessus)

---

**Bon test ! 🧪🚀**

