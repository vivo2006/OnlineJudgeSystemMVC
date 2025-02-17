document.addEventListener("DOMContentLoaded", function () {
    let pdfInput = document.getElementById("pdfFile");
    let pdfLabel = document.getElementById("fileLabel");
    let testCasesInput = document.getElementById("testCases");
    let testCasesLabel = document.getElementById("testCasesLabel");
    let form = document.getElementById("myForm");

    if (pdfInput && pdfLabel) {
        pdfInput.addEventListener("change", function () {
            pdfLabel.textContent = this.files.length > 0 ? this.files[0].name : "Изберете PDF файл";
        });
    }

    if (testCasesInput && testCasesLabel) {
        testCasesInput.addEventListener("change", function () {
            testCasesLabel.textContent = this.files.length > 0 ? `Избрани ${this.files.length} файла` : "Изберете тестови случаи";
        });
    }

    if (form) {
        form.addEventListener("submit", function (event) {
            if (!pdfInput.files.length || !testCasesInput.files.length) {
                alert("Моля, качете и двата файла.");
                event.preventDefault();
            }
        });
    }
});
