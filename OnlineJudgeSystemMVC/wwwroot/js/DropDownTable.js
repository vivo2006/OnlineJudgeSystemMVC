$(document).ready(function () {
    // Set default value
    const firstItem = $(".dropdown-item").first();
    const defaultId = firstItem.data("id"); // Get the data-id of the first item
    const defaultText = firstItem.text(); // Get the text of the first item

    // Update the dropdown button to display the first item's text
    $("#dropdownMenuButton").text(defaultText);

    loadTableData(defaultId);
    $(document).on('click', '.dropdown-item', function () {
        const selectedId = $(this).data('id'); // Get the selected year's ID
        const selectedText = $(this).text();  // Get the selected year's name
        $('#dropdownMenuButton').text(selectedText); // Update dropdown button text

        // Clear the table before appending new data
        $('#dataTable tbody').empty();

        loadTableData(selectedId);
    });
    function loadTableData(selectedId) {
        // Fetch the data for the selected year
        $.ajax({
            url: `/api/competitions/${selectedId}/years`, // Update with your actual API endpoint
            type: 'GET',
            success: function (data) {
                data.forEach(groupCompetition => {
                    // Prepare task links for each group
                    let taskLinks = "";
                    if (groupCompetition.problems.length != 0) taskLinks = groupCompetition.problems?.map().join("<br>"); // Separate task groups with a line break

                    // Add a new row to the table
                    const newRow = `
                    <tr>
                        <td>${groupCompetition.group}</td>
                        <td>${taskLinks}</td>
                    </tr>
                `;
                    $('#dataTable tbody').append(newRow);
                });
            },
            error: function (xhr, status, error) {
                console.error('Error fetching data:', error);
            }
        });
    }
});