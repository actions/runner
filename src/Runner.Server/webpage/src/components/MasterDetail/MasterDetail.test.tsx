import React from 'react';
import ReactDOM from 'react-dom';
import { MemoryRouter } from 'react-router-dom';
import { shallow, mount } from 'enzyme';
import { Simple } from '../Simple/Simple';   
import { stubMatchMedia } from 'utils-test';
import { MasterDetail } from './MasterDetail';

describe('<MasterDetail /> HOC', () => {

    let div: any;
    let originalMatchMedia: any;
    const masterProps = { description: 'Master' };
    const detailProps = { description: 'Detail' };

    const getTestSubject = () => {
        return (
            <MasterDetail MasterType={Simple} masterProps={masterProps}
                          DetailType={Simple} detailProps={detailProps} />
        );
    };

    beforeEach(() => {
        div = document.createElement('div');
        originalMatchMedia = window.matchMedia;
        window.matchMedia = stubMatchMedia(true); 
    });

    afterEach(() => {
        window.matchMedia = originalMatchMedia;
    });

    it('[SMOKE - DEEP] renders without crashing', () => {
        const testSubject = getTestSubject();
        ReactDOM.render(
            <MemoryRouter>
                {testSubject}
            </MemoryRouter>, div);
        ReactDOM.unmountComponentAtNode(div);
    });

    it('[SMOKE - SHALLOW] renders without crashing', () => {
        const testSubject = getTestSubject();
        shallow(
            <MemoryRouter>
                {testSubject}
            </MemoryRouter>);
    });

    describe('When on Smaller Screens', () => {
        
        let testSubject: any;
        let testInstance: any;  

        beforeEach(() => {
            testSubject = getTestSubject();
            window.matchMedia = stubMatchMedia(true); 
            testInstance = mount(
                <MemoryRouter>
                    {testSubject} 
                </MemoryRouter>);
        });

        it('should only display the master component initially', () => {
            const master = testInstance.find(`[data-test="Master"]`);
            expect(master.length).toEqual(1);
            const detail = testInstance.find(`[data-test="Detail"]`);
            expect(detail.length).toEqual(0);
        });

        it('the master components should get the expected props', () => {
            const master = testInstance.find(`[data-test="Master"]`);
            expect(master.props().description).toEqual(masterProps.description);
        });

        it('should display only the detail page when there is a matching route', () => {
            const detailTest = mount(
                <MemoryRouter initialEntries={['detail/9']}>
                    {testSubject}
                </MemoryRouter>);
            const master = detailTest.find(`[data-test="Master"]`);
            expect(master.length).toEqual(0);    
        });

    });

    describe('When on Larger Screens', () => {

        let testSubject: any;
        let testInstance: any;

        beforeEach(() => {
            testSubject = getTestSubject();
            window.matchMedia = stubMatchMedia(false); 
            testInstance = mount(
                <MemoryRouter>
                    {testSubject}
                </MemoryRouter>);
        });

        it('master and detail components should both be displayed', () => {
            const master = testInstance.find(`[data-test="Master"]`);
            expect(master.length).toEqual(1);
            const detail = testInstance.find(`[data-test="Detail"]`);
            expect(detail.length).toEqual(1);
        });

        it('master and detail components should both get the expected props', () => {
            const master = testInstance.find(`[data-test="Master"]`);
            expect(master.props().description).toEqual(masterProps.description);
            const detail = testInstance.find(`[data-test="Detail"]`);
            expect(detail.props().description).toEqual(detailProps.description);
        });

    });

});