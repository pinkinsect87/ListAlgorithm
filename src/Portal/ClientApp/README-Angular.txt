Angular Cheat Sheet

ng generate component NAMEOFCOMPONENT --skip-import
// Be sure to be in the ClientApp folder where you want to add your component
// For instance: C:\ZCode\Atlas\AtlasOverallSolution\Development\Src\AtlasOverallSolution\Portal\ClientApp

// If you see this message "More than one module matches. Use skip-import option
// to skip importing the component into the closest module." use the --skip-import option.
// However you will then need to make sure you go to the ClientApp/src/app/app.module.ts and add
// to the import and declaration sections

ng build --prod
Option "--prod" is deprecated: Use "--configuration production" instead.
// Run this to do a full build which is more likely to show errors that the
// normal Microsoft VS build won't. Especially if you see that the page doesn't come up
// and instead of you see a small bit of text on the screen.

// If someone else has installed components you might need to run these commands so that your
// computer is updated with the correct components

npm install

npm audit
npm audit fix
