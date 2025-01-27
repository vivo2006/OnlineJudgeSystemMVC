$(document).ready(function () {
    // When a competition button is clicked
    $('.btn-open-modal').click(function () {
        const competitionId = $(this).data('id');
        const competitionName = $(this).data('name');

        // Set the modal title
        $('#yearsModalLabel').text(`Години`);

        // Fetch years for the competition
        $.ajax({
            url: `/api/competitions/${competitionId}/years`, // Update with your API endpoint
            type: 'GET',
            success: function (data) {
                const yearsList = $('#yearsList');
                yearsList.empty(); // Clear previous years

                data.sort((a, b) => a.item2 - b.item2);

                // Populate the list with years
                data.forEach(year => {
                    const yearItem = `
                                    <li class="list-group-item d-flex flex-row justify-content-center">
                                        <a href="/GroupCompetitions/Index/?competitionId=${competitionId}&yearId=${year.item1}" class="text-center align-middle" style="color:#ED2A4F; margin-left:20%">
                                            ${year.item2}
                                        </a>
                                        <a class="text-dark mr-5" href="/YearCompetitions/Edit/${year.item1}" style="margin-left:8%">✏️</a>
                                        <a class="text-dark mr-5" href="/YearCompetitions/Delete/${year.item1}" style="margin-left:2%">🗑️</a>
                                    </li>`;
                    yearsList.append(yearItem);


                });
                const yearItemLast = `
                                    <li class="list-group-item">
                        <a href="/YearCompetitions/Create/${competitionId}">+</a>
                    </li>`;
                yearsList.append(yearItemLast);
            },
            error: function (xhr, status, error) {
                console.error('Error fetching years:', error);
            }
        });
    });
});