document.addEventListener("DOMContentLoaded", function () {
    let pdfInput = document.getElementById("pdfFile");
    let pdfLabel = document.getElementById("fileLabel");
    let testCasesInput = document.getElementById("testCases");
    let testCasesLabel = document.getElementById("testCasesLabel");
    let form = document.getElementById("myForm");

    if (pdfInput && pdfLabel) {
        pdfInput.addEventListener("change", function () {
            pdfLabel.textContent = this.files.length > 0 ? this.files[0].name : "Choose a PDF file";
        });
    }

    if (testCasesInput && testCasesLabel) {
        testCasesInput.addEventListener("change", function () {
            testCasesLabel.textContent = this.files.length > 0 ? `${this.files.length} files selected` : "Choose test cases";
        });
    }

    if (form) {
        form.addEventListener("submit", function (event) {
            if (!pdfInput.files.length || !testCasesInput.files.length) {
                alert("Please upload a problem statement and a test-case set.");
                event.preventDefault();
            }
        });
    }
});
